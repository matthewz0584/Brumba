using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.DsspUtils;
using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using OdometryPxy = Brumba.DiffDriveOdometry.Proxy;
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
            SpawnIterator(StartIt);
        }

        IEnumerator<ITask> StartIt()
        {
            //_timerFacade = new TimerFacade(this, _state.DeltaT);

            //OccupancyGrid map = null;
            //yield return _mapProvider.Get().Receive(ms => map = (OccupancyGrid)DssTypeHelper.TransformFromProxy(ms.Map));
            //map.Freeze();

            //var sparsifiedRp = _state.RangeFinderProperties.Sparsify(_state.BeamsNumber);
            //_takeEachNthBeam = (int)Math.Round(sparsifiedRp.AngularResolution / _state.RangeFinderProperties.AngularResolution);
            //_localizer = new McLrfLocalizer(map, sparsifiedRp, _state.ParticlesNumber);

            //if (IsPoseUnknown(_state.FirstPoseCandidate))
            //    _localizer.InitPoseUnknown();
            //else
            //    _localizer.InitPose(_state.FirstPoseCandidate, new Pose(new Vector2(0.3f, 0.3f), 10 * Constants.Degree));

            //yield return _odometryProvider.Get().Receive(os => _currentOdometry = (Pose)DssTypeHelper.TransformFromProxy(os.State.Pose));

            //base.Start();

            //MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
            //        Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateLocalizer))));

            yield return To.Exec(() => _timerFacade.Set());
        }

        IEnumerator<ITask> UpdateLocalizer(TimeSpan dt)
        {
            //yield return Arbiter.JoinedReceive<SickLrfPxy.State, OdometryPxy.DiffDriveOdometryServiceState>(false,
            //    _lrf.Get(), _odometryProvider.Get(),
            //    (lrfScan, odometry) =>
            //    {
            //        DC.Contract.Requires(lrfScan != null);
            //        DC.Contract.Requires(lrfScan.DistanceMeasurements != null);
            //        DC.Contract.Requires(odometry != null);
            //        DC.Contract.Requires(odometry.State != null);

            //        var sw = Stopwatch.StartNew();

            //        var newOdometry = (Pose)DssTypeHelper.TransformFromProxy(odometry.State.Pose);
            //        _localizer.Update(newOdometry - _currentOdometry, PreprocessLrfScan(lrfScan));
            //        _currentOdometry = newOdometry;

            //        UpdateState();
            //        LogInfo("loc {0} for {1}", _state.FirstPoseCandidate, sw.Elapsed.Milliseconds);
            //    });

            yield break;

            //yield return To.Exec(Draw);
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

        //IEnumerable<float> PreprocessLrfScan(SickLrfPxy.State lrfScan)
        //{
        //    DC.Contract.Requires(lrfScan != null);
        //    DC.Contract.Ensures(DC.Contract.Result<IEnumerable<float>>().Count() == _state.BeamsNumber);

        //    return lrfScan.DistanceMeasurements.Where((d, i) => i % _takeEachNthBeam == 0).Select(d => d / 1000f).Take(_state.BeamsNumber);
        //}
    }
}