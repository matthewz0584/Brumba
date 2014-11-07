using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.McLrfLocalizer;
using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using OdometryPxy = Brumba.DiffDriveOdometry.Proxy;
using LocalizerPxy = Brumba.GenericLocalizer.Proxy;
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

        [Partner("Odometry", Contract = OdometryPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        OdometryPxy.DiffDriveOdometryOperations _odometryProvider = new OdometryPxy.DiffDriveOdometryOperations();

        [Partner("Localizer", Contract = LocalizerPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        LocalizerPxy.GenericLocalizerOperations _localizer = new LocalizerPxy.GenericLocalizerOperations();

        [Partner("Lrf", Contract = SickLrfPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SickLrfPxy.SickLRFOperations _lrf = new SickLrfPxy.SickLRFOperations();

        [Partner("DifferentialDrive", Contract = DrivePxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        DrivePxy.DriveOperations _drive = new DrivePxy.DriveOperations();

        DwaNavigator _dwaNavigator;
        
        int _takeEachNthBeam;
        TimerFacade _timerFacade;

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

        IEnumerator<ITask> UpdateNavigator(TimeSpan _)
        {
            yield return new JoinReceiver(false, new Task<SickLrfPxy.State, OdometryPxy.DiffDriveOdometryServiceState, LocalizerPxy.GenericLocalizerState>(
                (lrfScan, odometry, localization) =>
                {
                    DC.Contract.Requires(lrfScan != null);
                    DC.Contract.Requires(lrfScan.DistanceMeasurements != null);
                    DC.Contract.Requires(odometry != null);
                    DC.Contract.Requires(odometry.State != null);

                    var pose = (Pose)DssTypeHelper.TransformFromProxy(localization.EstimatedPose);
                    var velocity = (Pose)DssTypeHelper.TransformFromProxy(odometry.State.Velocity);

                    _dwaNavigator.Update(pose, velocity, _state.Target, PreprocessLrfScan(lrfScan));

                    _state.CurrentVelocityAcceleration = _dwaNavigator.OptimalVelocity;
                    _state.VelocititesEvaluation = _dwaNavigator.VelocitiesEvaluation.ToArray();

                    _drive.SetDrivePower(_state.CurrentVelocityAcceleration.WheelAcceleration.X,
                        _state.CurrentVelocityAcceleration.WheelAcceleration.Y);
                }),
                (IPortReceive)_lrf.Get(), (IPortReceive)_odometryProvider.Get(), (IPortReceive)_localizer.Get());
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

        IEnumerable<float> PreprocessLrfScan(SickLrfPxy.State lrfScan)
        {
            DC.Contract.Requires(lrfScan != null);

            return lrfScan.DistanceMeasurements.Where((d, i) => i % _takeEachNthBeam == 0).Select(d => d / 1000f);
        }
    }
}