using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.WaiterStupid.Odometry;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using W3C.Soap;
using DC = System.Diagnostics.Contracts;
using SickLrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;

namespace Brumba.WaiterStupid.McLocalization
{
    [Contract(Contract.Identifier)]
    [DisplayName("Brumba MonteCarlo Localizer")]
    [Description("no description provided")]
    public class McLrfLocalizerService : DsspServiceExposing
    {
        [ServiceState]
        McLrfLocalizerState _state = new McLrfLocalizerState
        {
            FirstPoseCandidate = new Pose(new Vector2(), float.NaN),
            RangeFinderProperties = new RangefinderProperties
            {
                AngularResolution = Constants.Degree,
                AngularRange = Constants.Pi,
                MaxRange = 10,
                OriginPose = new Pose(new Vector2(), -Constants.PiOver2)
            },
            Map = new OccupancyGrid(new bool[100, 100], 0.1f),
            ParticlesNumber = 100,
            DeltaT = 0.3f
        };

        [ServicePort("/McLrfLocalizer", AllowMultipleInstances = true)]
        McLrfLocalizerOperations _mainPort = new McLrfLocalizerOperations();

        [Partner("Odometry", Contract = Odometry.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        DiffDriveOdometryOperations _odometry = new DiffDriveOdometryOperations();

        [Partner("Lrf", Contract = SickLrfPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SickLrfPxy.SickLRFOperations _lrf = new SickLrfPxy.SickLRFOperations();

        McLrfLocalizer _localizer;
        TimerFacade _timerFacade;
        Pose _currentOdometry;

        public McLrfLocalizerService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        protected override void Start()
        {
            InitFromState(_state);

            base.Start();

            SpawnIterator(Restart);
        }

        IEnumerator<ITask> Restart()
        {
            yield return GetOdometry().Receive(os =>
            {
                _currentOdometry = (Pose) DssTypeHelper.TransformFromProxy(os.State.Pose);
            });

            //To synchronize UpdateLocalizer with Replace
            MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
                    Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateLocalizer))));

            yield return To.Exec(() => _timerFacade.Set());
        }
       
        IEnumerator<ITask> UpdateLocalizer(TimeSpan dt)
        {
            yield return Arbiter.JoinedReceive(false, _lrf.Get().P0, GetOdometry().P0, 
                (lrfScan, odometry) =>
                    {
                        _localizer.Update((Pose)DssTypeHelper.TransformFromProxy(odometry) - _currentOdometry,
                                            lrfScan.DistanceMeasurements.Select(d => d / 100f));
                    });
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public IEnumerator<ITask> OnReplace(Replace replaceRq)
        {
            _timerFacade.Dispose();

            InitFromState(replaceRq.Body);

            yield return To.Exec(Restart);

            replaceRq.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public void OnDropDown(DsspDefaultDrop dropDownRq)
        {
            DC.Contract.Requires(dropDownRq != null);
            DC.Contract.Requires(dropDownRq.Body != null);

            _timerFacade.Dispose();
            DefaultDropHandler(dropDownRq);
        }

        void InitFromState(McLrfLocalizerState state)
        {
            _state = state;

            _localizer = new McLrfLocalizer(_state.Map, _state.RangeFinderProperties, _state.ParticlesNumber);
            if (double.IsNaN(_state.FirstPoseCandidate.Bearing))
                _localizer.InitPoseUnknown();
            else
                _localizer.InitPose(_state.FirstPoseCandidate, new Pose(new Vector2(0.3f, 0.3f), 10 * Constants.Degree));

            _timerFacade = new TimerFacade(this, _state.DeltaT);
        }

        //This convenience method is implemented in Proxy, but I can not refer to proxy of the very assembly, waiting for the separation
        PortSet<DiffDriveOdometryServiceState, Fault> GetOdometry()
        {
            var get = new Odometry.Get(new GetRequestType());
            _odometry.Post(get);
            return get.ResponsePort;
        }
    }
}