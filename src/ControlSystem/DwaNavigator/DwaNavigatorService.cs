using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brumba.Common;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using LocalizerPxy = Brumba.GenericLocalizer.Proxy;
using VelocimeterPxy = Brumba.GenericFixedWheelVelocimeter.Proxy;
using SickLrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    [Contract(Contract.Identifier)]
    [DisplayName("Brumba DWA Navigator")]
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

        [Partner("Velocimeter", Contract = VelocimeterPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        VelocimeterPxy.GenericFixedWheelVelocimeterOperations _velocimeter = new VelocimeterPxy.GenericFixedWheelVelocimeterOperations();

        [Partner("Localizer", Contract = LocalizerPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        LocalizerPxy.GenericLocalizerOperations _localizer = new LocalizerPxy.GenericLocalizerOperations();

        [Partner("Lrf", Contract = SickLrfPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SickLrfPxy.SickLRFOperations _lrf = new SickLrfPxy.SickLRFOperations();

        [Partner("DifferentialDrive", Contract = DrivePxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        DrivePxy.DriveOperations _drive = new DrivePxy.DriveOperations();

        DwaBootstrapper _dwaBootstrapper;
        
        int _takeEachNthBeam;
        TimerFacade _timerFacade;

        public DwaNavigatorService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
            DC.Contract.Requires(creationPort != null);
        }

        protected override void Start()
        {
            _state.VelocititesEvaluation = new double[1, 1];

            SpawnIterator(StartIt);
        }

        IEnumerator<ITask> StartIt()
        {
            _timerFacade = new TimerFacade(this, _state.DeltaT);

            _dwaBootstrapper = new DwaBootstrapper(_state.RobotMass, _state.RobotInertiaMoment, _state.WheelRadius, _state.WheelBase, _state.RobotRadius,
                _state.VelocityMax, _state.BreakageDeceleration, _state.CurrentToTorque, _state.FrictionTorque,
                _state.RangefinderProperties, _state.LaneWidthCoef, _state.DeltaT);

            _takeEachNthBeam = 1;

            base.Start();

            MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
                Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateNavigator))));

            yield return To.Exec(() => _timerFacade.Set());
            _timerFacade.TickPort.Post(new TimeSpan());
        }

        IEnumerator<ITask> UpdateNavigator(TimeSpan _)
        {
            yield return JoinedReceive<SickLrfPxy.State, VelocimeterPxy.GenericFixedWheelVelocimeterState, LocalizerPxy.GenericLocalizerState>(
                false, _lrf.Get(), _velocimeter.Get(), _localizer.Get(),
                (lrfScan, velocimeterSt, localizerSt) =>
                {
                    DC.Contract.Requires(lrfScan != null);
                    DC.Contract.Requires(lrfScan.DistanceMeasurements != null);
                    DC.Contract.Requires(velocimeterSt != null);
                    DC.Contract.Requires(localizerSt != null);

                    //Defective lrf scan
                    if (_state.Iteration++ < 1)
                        return;

                    var pose = (Pose)DssTypeHelper.TransformFromProxy(localizerSt.EstimatedPose);
                    var velocity = (Velocity)DssTypeHelper.TransformFromProxy(velocimeterSt.Velocity);

                    _dwaBootstrapper.Update(pose, velocity, _state.Target, PreprocessLrfMeasurements(lrfScan.DistanceMeasurements));

                    _state.CurrentVelocityAcceleration = _dwaBootstrapper.OptimalVelocity;
                    _state.VelocititesEvaluation = _dwaBootstrapper.VelocitiesEvaluation.ToArray();
                    //_state.Iteration ++;

                    _drive.SetDrivePower(_state.CurrentVelocityAcceleration.WheelAcceleration.X,
                        _state.CurrentVelocityAcceleration.WheelAcceleration.Y);
                });
        }

        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public void OnSetTarget(SetTarget setTargetRq)
        {
            DC.Contract.Requires(setTargetRq != null);
            DC.Contract.Requires(setTargetRq.Body != null);
            DC.Contract.Requires(setTargetRq.ResponsePort != null);

            _state.Target = setTargetRq.Body.Target;
            _drive.EnableDrive(true);

            setTargetRq.ResponsePort.Post(new DefaultUpdateResponseType());
        }

        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public void OnDropDown(DsspDefaultDrop dropDownRq)
        {
            DC.Contract.Requires(dropDownRq != null);
            DC.Contract.Requires(dropDownRq.Body != null);

            _drive.EnableDrive(false);
            _timerFacade.Dispose();
            DefaultDropHandler(dropDownRq);
        }

        IEnumerable<float> PreprocessLrfMeasurements(int[] distanceMeasurements)
        {
            DC.Contract.Requires(distanceMeasurements != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<float>>().Count() == distanceMeasurements.Length);

            return distanceMeasurements.Where((d, i) => i % _takeEachNthBeam == 0).Select(d => d / 1000f);
        }

        static JoinReceiver JoinedReceive<T0, T1, T2>(bool persist, Port<T0> port0, Port<T1> port1, Port<T2> port2, Handler<T0, T1, T2> handler)
        {
            return new JoinReceiver(persist, new Task<T0, T1, T2>(handler), new IPortReceive[] { port0, port1, port2 });
        }
    }
}