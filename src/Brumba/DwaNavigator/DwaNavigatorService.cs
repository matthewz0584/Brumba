using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using OdometryPxy = Brumba.DiffDriveOdometry.Proxy;
using McLrfLocalizerPxy = Brumba.McLrfLocalizer.Proxy;
using SickLrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    [Contract(Contract.Identifier)]
    [DisplayName("Brumba MonteCarlo Localizer")]
    [Description("no description provided")]
    public class DwaNavigatorService : DsspServiceExposing
    {
#pragma warning disable 0649
        [ServiceState]
        [InitialStatePartner(Optional = false)]
        DwaNavigatorState _state;
#pragma warning restore 0649

        [ServicePort("/DwaNavigator", AllowMultipleInstances = true)]
        DwaNavigatorOperations _mainPort = new DwaNavigatorOperations();

        [Partner("Odometry", Contract = OdometryPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        OdometryPxy.DiffDriveOdometryOperations _odometryProvider = new OdometryPxy.DiffDriveOdometryOperations();

        [Partner("McLrfLocalizer", Contract = McLrfLocalizerPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        McLrfLocalizerPxy.McLrfLocalizerOperations _localizer = new McLrfLocalizerPxy.McLrfLocalizerOperations();

        [Partner("Lrf", Contract = SickLrfPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SickLrfPxy.SickLRFOperations _lrf = new SickLrfPxy.SickLRFOperations();

        [Partner("Drive", Contract = DrivePxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        DrivePxy.DriveOperations _drive = new DrivePxy.DriveOperations();

        DwaNavigator _dwaNavigator;
        
        int _takeEachNthBeam;
        TimerFacade _timerFacade;
        Pose _currentOdometry;

        public DwaNavigatorService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
            DC.Contract.Requires(creationPort != null);
        }

        protected override void Start()
        {
            _dwaNavigator = new DwaNavigator(_state.WheelAngularAccelerationMax, _state.WheelAngularVelocityMax,
                _state.WheelRadius, _state.WheelBase, _state.RobotRadius, _state.RangefinderProperties, _state.DeltaT);

            _timerFacade = new TimerFacade(this, _state.DeltaT);

            _takeEachNthBeam = 1;

            base.Start();

            MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
                    Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateNavigator))));

            To.Exec(() => _timerFacade.Set());
        }

        IEnumerator<ITask> UpdateNavigator(TimeSpan dt)
        {
    //        public static JoinReceiver JoinedReceive<T0, T1>(bool persist, Port<T0> port0, Port<T1> port1, Handler<T0, T1> handler)
    //{
    //  return new JoinReceiver((persist ? 1 : 0) != 0, (ITask) new Task<T0, T1>(handler), new IPortReceive[2]
    //  {
    //    (IPortReceive) port0,
    //    (IPortReceive) port1
    //  });
    //}
            yield return new JoinReceiver(false, new Task<SickLrfPxy.State, OdometryPxy.DiffDriveOdometryServiceState, Pose>(
                (lrfScan, odometry, pose) =>
                {
                    DC.Contract.Requires(lrfScan != null);
                    DC.Contract.Requires(lrfScan.DistanceMeasurements != null);
                    DC.Contract.Requires(odometry != null);
                    DC.Contract.Requires(odometry.State != null);

                    var velocity = (Pose)DssTypeHelper.TransformFromProxy(odometry.State.Velocity);
                    _dwaNavigator.Update(pose, velocity, _state.Target, PreprocessLrfScan(lrfScan));
                    
                    //UpdateState();
                }),
                (IPortReceive)_lrf.Get(), (IPortReceive)_odometryProvider.Get(), (IPortReceive)_localizer.QueryPose());
        }

        //[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        //public void OnQueryPose(QueryPose queryPoseRq)
        //{
        //    DC.Contract.Requires(queryPoseRq != null);
        //    DC.Contract.Requires(queryPoseRq.Body != null);
        //    DC.Contract.Requires(queryPoseRq.ResponsePort != null);

        //    queryPoseRq.ResponsePort.Post(_state.FirstPoseCandidate);
        //}

        //[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        //public void InitPoseRequest(InitPose initPoseRq)
        //{
        //    DC.Contract.Requires(initPoseRq != null);
        //    DC.Contract.Requires(initPoseRq.Body != null);
        //    DC.Contract.Requires(initPoseRq.ResponsePort != null);

        //    _localizer.InitPose(initPoseRq.Body.Pose, new Pose(new Vector2(0.3f, 0.3f), 10 * Constants.Degree));
        //    UpdateState();
        //    initPoseRq.ResponsePort.Post(new DefaultUpdateResponseType());
        //}

        //[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        //public void InitPoseUnknownRequest(InitPoseUnknown initPoseUnknownRq)
        //{
        //    DC.Contract.Requires(initPoseUnknownRq != null);
        //    DC.Contract.Requires(initPoseUnknownRq.Body != null);
        //    DC.Contract.Requires(initPoseUnknownRq.ResponsePort != null);

        //    _localizer.InitPoseUnknown();
        //    UpdateState();
        //    initPoseUnknownRq.ResponsePort.Post(new DefaultUpdateResponseType());
        //}

        //[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        //public IEnumerator<ITask> OnSubscribe(Subscribe subscribeRq)
        //{
        //    DC.Contract.Requires(subscribeRq != null);
        //    DC.Contract.Requires(subscribeRq.Body != null);
        //    DC.Contract.Requires(subscribeRq.ResponsePort != null);

        //    yield return SubscribeHelper(_subMgrPort, subscribeRq.Body, subscribeRq.ResponsePort).Choice(success => { }, LogError);
        //}

        //[ServiceHandler(ServiceHandlerBehavior.Teardown)]
        //public void OnDropDown(DsspDefaultDrop dropDownRq)
        //{
        //    DC.Contract.Requires(dropDownRq != null);
        //    DC.Contract.Requires(dropDownRq.Body != null);

        //    _timerFacade.Dispose();
        //    DefaultDropHandler(dropDownRq);
        //}

        //void UpdateState()
        //{
        //    //_state.FirstPoseCandidate = _localizer.GetPoseCandidates().First();
        //    _state.FirstPoseCandidate = _localizer.CalculatePoseMean();
        //    SendNotification(_subMgrPort, new InitPose { Body = { Pose = _state.FirstPoseCandidate } });
        //}

        //bool IsPoseUnknown(Pose pose)
        //{
        //    return double.IsNaN(pose.Bearing);
        //}

        IEnumerable<float> PreprocessLrfScan(SickLrfPxy.State lrfScan)
        {
            DC.Contract.Requires(lrfScan != null);

            return lrfScan.DistanceMeasurements.Where((d, i) => i % _takeEachNthBeam == 0).Select(d => d / 1000f);
        }
    }
}