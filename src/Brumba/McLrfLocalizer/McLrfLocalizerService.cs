using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;
using SickLrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;
using OdomPxy = Brumba.DiffDriveOdometry.Proxy;

namespace Brumba.McLrfLocalizer
{
    [Contract(Contract.Identifier)]
    [DisplayName("Brumba MonteCarlo Localizer")]
    [Description("no description provided")]
    public class McLrfLocalizerService : DsspServiceExposing
    {
        [ServiceState]
		[InitialStatePartner(Optional = false)]
        McLrfLocalizerState _state;

        [ServicePort("/McLrfLocalizer", AllowMultipleInstances = true)]
        McLrfLocalizerOperations _mainPort = new McLrfLocalizerOperations();

        [Partner("Odometry", Contract = OdomPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        OdomPxy.DiffDriveOdometryOperations _odometry = new OdomPxy.DiffDriveOdometryOperations();

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
	        _localizer = new McLrfLocalizer(_state.Map, _state.RangeFinderProperties, _state.ParticlesNumber);
	        if (IsPoseUnknown(_state.FirstPoseCandidate))
		        _localizer.InitPoseUnknown();
	        else
		        _localizer.InitPose(_state.FirstPoseCandidate, new Pose(new Vector2(0.3f, 0.3f), 10 * Constants.Degree));

	        _timerFacade = new TimerFacade(this, _state.DeltaT);

	        base.Start();

            SpawnIterator(StartIt);
        }


	    IEnumerator<ITask> StartIt()
        {
            yield return _odometry.Get().Receive(os =>
            {
                _currentOdometry = (Pose) DssTypeHelper.TransformFromProxy(os.State.Pose);
            });

            MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
                    Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateLocalizer))));

            yield return To.Exec(() => _timerFacade.Set());
        }
       
        IEnumerator<ITask> UpdateLocalizer(TimeSpan dt)
        {
            yield return Arbiter.JoinedReceive(false, _lrf.Get().P0, _odometry.Get().P0, 
                (lrfScan, odometry) =>
                    {
                        _localizer.Update((Pose)DssTypeHelper.TransformFromProxy(odometry) - _currentOdometry,
                                            lrfScan.DistanceMeasurements.Select(d => d / 100f));
                    });
        }

        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public void OnDropDown(DsspDefaultDrop dropDownRq)
        {
            DC.Contract.Requires(dropDownRq != null);
            DC.Contract.Requires(dropDownRq.Body != null);

            _timerFacade.Dispose();
            DefaultDropHandler(dropDownRq);
        }

		bool IsPoseUnknown(Pose pose)
		{
			return double.IsNaN(pose.Bearing);
		}
    }
}