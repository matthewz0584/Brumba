using System.ComponentModel;
using Microsoft.Dss.Core.Attributes;
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
		readonly ReferencePlatform2011State _state = new ReferencePlatform2011State
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
        ReferencePlatform2011Operations _mainPort = new ReferencePlatform2011Operations();

		[SubscriptionManagerPartner("SubMgr")]
		Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort _subMgrPort = new Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedReferencePlatform2011Service"/> class.
        /// </summary>
        /// <param name="creationPort">The creation port.</param>
        public SimulatedReferencePlatform2011Service(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
        {
        }

        protected override void OnInsertEntity()
        {
            _state.DriveState.DistanceBetweenWheels = RpEntity.ChassisDimensions.X;
            _state.DriveState.LeftWheel.MotorState.PowerScalingFactor = RpEntity.MotorTorqueScaling;
            _state.DriveState.RightWheel.MotorState.PowerScalingFactor = RpEntity.MotorTorqueScaling;
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public void OnSubscribe(Subscribe subscribe)
        {
			if (FaultIfNotConnected(subscribe))
				return;
            LogInfo("SimulatedReferencePlatform2011Service.Subscribe NOT IMPLEMENTED");
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public void OnGet(Get get)
        {
			if (IsConnected)
				UpdateStateFromSimulation();

            DefaultGetHandler(get);
        }

	    [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
	    public void OnUpdateWheelTicksSigma(UpdateWheelTicksSigma upd)
	    {
		    if (FaultIfNotConnected(upd))
			    return;
		    _state.WheelTicksSigma = upd.Body.WheelTicksSigma;
			upd.ResponsePort.Post(new DefaultUpdateResponseType());
	    }

		protected override IConnectable GetState() { return _state; }

        ReferencePlatform2011Entity RpEntity { get { return Entity as ReferencePlatform2011Entity; } }
    }
}