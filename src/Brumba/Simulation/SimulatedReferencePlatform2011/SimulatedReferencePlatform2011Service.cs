using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using drive = Microsoft.Robotics.Services.Drive;
using battery = Microsoft.Robotics.Services.Battery;
using Microsoft.Robotics.Services.Motor;
using Microsoft.Robotics.Services.Encoder;

namespace Brumba.Simulation.SimulatedReferencePlatform2011
{
    /// <summary>
    /// Simulated 2011 Reference platform service
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(drive.Contract.Identifier)]
    [AlternateContract(battery.Contract.Identifier)]
    [DisplayName("Brumba Simulated Reference Platform Robot 2011")]
    [Description("Simulated Reference Platform 2011 for Robotics Developer Studio")]
    partial class SimulatedReferencePlatform2011Service : SimulatedEntityServiceBase
    {
	    /// <summary>
	    /// Service state
	    /// </summary>
	    [ServiceState]
		private ReferencePlatform2011State _state = new ReferencePlatform2011State
		    {
			    DriveState = new drive.DriveDifferentialTwoWheelState
				    {
					    LeftWheel = new WheeledMotorState
						    {
							    MotorState = new MotorState(),
							    EncoderState = new EncoderState()
						    },
					    RightWheel = new WheeledMotorState
						    {
							    MotorState = new MotorState(),
							    EncoderState = new EncoderState()
						    }
				    },
			    BatteryState = new battery.BatteryState
					    {
						    MaxBatteryPower = 12, PercentBatteryPower = 80, PercentCriticalBattery = 20
					    }
		    };

        /// <summary>
        /// Main service port
        /// </summary>
        [ServicePort("/SimulatedReferencePlatform2011", AllowMultipleInstances = true)]
        private ReferencePlatform2011Operations _mainPort = new ReferencePlatform2011Operations();

		[SubscriptionManagerPartner("SubMgr")]
		private Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort _subMgrPort = new Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedReferencePlatform2011Service"/> class.
        /// </summary>
        /// <param name="creationPort">The creation port.</param>
        public SimulatedReferencePlatform2011Service(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
        {
        }

        protected override Interleave ConcreteWaitingInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                    Arbiter.Receive<DsspDefaultDrop>(false, _drivePort, DefaultDropHandler),
                    Arbiter.Receive<DsspDefaultDrop>(false, _batteryPort, DefaultDropHandler)),
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                    Arbiter.Receive<DsspDefaultLookup>(true, _drivePort, DefaultLookupHandler),
                    Arbiter.Receive<DsspDefaultLookup>(true, _batteryPort, DefaultLookupHandler),
                    Arbiter.Receive<Get>(true, _mainPort, DefaultGetHandler))
                );
        }

        protected override Interleave ConcreteActiveInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                    Arbiter.Receive<DsspDefaultDrop>(false, _drivePort, DefaultDropHandler),
                    Arbiter.Receive<DsspDefaultDrop>(false, _batteryPort, DefaultDropHandler)
                    ),
                new ExclusiveReceiverGroup(
                    Arbiter.Receive<drive.ResetEncoders>(true, _drivePort, ResetEncodersHandler),
                    Arbiter.Receive<drive.DriveDistance>(true, _drivePort, DriveDistanceHandler),
                    Arbiter.Receive<drive.RotateDegrees>(true, _drivePort, DriveRotateHandler),
                    Arbiter.Receive<drive.SetDrivePower>(true, _drivePort, DriveSetPowerHandler),
                    Arbiter.Receive<drive.SetDriveSpeed>(true, _drivePort, DriveSetSpeedHandler),
                    Arbiter.Receive<drive.AllStop>(true, _drivePort, DriveAllStopHandler)
                    ),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                    Arbiter.Receive<Get>(true, _mainPort, OnGet),
                    Arbiter.Receive<Subscribe>(true, _mainPort, OnSubscribe),
                    
                    Arbiter.Receive<DsspDefaultLookup>(true, _drivePort, DefaultLookupHandler),
                    Arbiter.Receive<drive.Get>(true, _drivePort, DriveGetHandler),
					Arbiter.Receive<HttpGet>(true, _drivePort, DriveHttpGetHandler),
                    Arbiter.ReceiveWithIterator<drive.Subscribe>(true, _drivePort, DriveSubscribeHandler),
                    Arbiter.ReceiveWithIterator<drive.ReliableSubscribe>(true, _drivePort, DriveReliableSubscribeHandler),
                    Arbiter.Receive<drive.EnableDrive>(true, _drivePort, DriveEnableHandler),

                    Arbiter.Receive<DsspDefaultLookup>(true, _batteryPort, DefaultLookupHandler),
                    Arbiter.Receive<battery.Get>(true, _batteryPort, BatteryGetHandler),
					Arbiter.Receive<HttpGet>(true, _batteryPort, BatteryHttpGetHandler),
                    Arbiter.Receive<battery.Replace>(true, _batteryPort, ReplaceHandler),
                    Arbiter.ReceiveWithIterator<battery.Subscribe>(true, _batteryPort, SubscribeHandler),
                    Arbiter.Receive<battery.SetCriticalLevel>(true, _batteryPort, SetCriticalLevelHandler)
                    ));
        }

        protected override void OnInsertEntity()
        {
            LogInfo("SimulatedReferencePlatform2011Service OnInsertEntity called");

            if (RpEntity.ChassisShape != null)
            {
                _state.DriveState.DistanceBetweenWheels = RpEntity.ChassisShape.BoxState.Dimensions.X;
            }
            _state.DriveState.LeftWheel.MotorState.PowerScalingFactor = RpEntity.MotorTorqueScaling;
            _state.DriveState.RightWheel.MotorState.PowerScalingFactor = RpEntity.MotorTorqueScaling;
        }

        protected override void OnDeleteEntity()
        {
            LogInfo("SimulatedReferencePlatform2011Service OnDeleteEntity called");
        }

        void OnSubscribe(Subscribe subscribe)
        {
            LogInfo("SimulatedReferencePlatform2011Service.Subscribe NOT IMPLEMENTED");
        }

        void OnGet(Get get)
        {
            UpdateStateFromSimulation();

            DefaultGetHandler(get);
        }

        ReferencePlatform2011Entity RpEntity { get { return Entity as ReferencePlatform2011Entity; } }
    }
}