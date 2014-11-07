using System.ComponentModel;
using Brumba.AckermanVehicle;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    [Contract(Contract.Identifier)]
    [DisplayName("Brumba Simulated Ackerman Vehicle")]
    [Description("no description provided")]
    class SimulatedAckermanVehicleService : SimulatedEntityServiceBase
    {
        [ServiceState]
        readonly SimulatedAckermanVehicleState _state = new SimulatedAckermanVehicleState();

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = AckermanVehicle.Contract.Identifier)]
        private AckermanVehicleOperations _ackermanVehiclePort = new AckermanVehicleOperations();

        [ServicePort("/SimulatedAckermanVehicle", AllowMultipleInstances = true)]
        private SimulatedAckermanVehicleOperations _mainPort = new SimulatedAckermanVehicleOperations();

        public SimulatedAckermanVehicleService(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
        {
        }

        protected override void OnDeleteEntity()
        {
            _state.DriveAngularDistance = 0;
            _state.SteeringAngle = 0;
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_ackermanVehiclePort")]
        public void OnUpdateDrivePower(UpdateDrivePower powerRequest)
		{
		    if (FaultIfNotConnected(powerRequest))
		        return;

            Vehicle.SetDrivePower(powerRequest.Body.Value);

            powerRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_ackermanVehiclePort")]
		public void OnUpdateSteeringAngle(UpdateSteeringAngle steeringRequest)
        {
            if (FaultIfNotConnected(steeringRequest))
                return;

            Vehicle.SetSteeringAngle(steeringRequest.Body.Value);

            steeringRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_ackermanVehiclePort")]
        public void OnBreak(Break breakRequest)
        {
            if (FaultIfNotConnected(breakRequest))
                return;

            Vehicle.Break();

            breakRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_ackermanVehiclePort")]
		public void OnGet(AckermanVehicle.Get get)
        {
			if (IsConnected)
				UpdateState();

            DefaultGetHandler(get);
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public void OnGet(Get get)
        {
			if (IsConnected)
				UpdateState();

            DefaultGetHandler(get);
        }

        void UpdateState()
        {
            _state.DriveAngularDistance = Vehicle.GetDriveAngularDistance();
            _state.SteeringAngle = Vehicle.GetSteeringAngle();
        }

		protected override IConnectable GetState() { return _state; }

		AckermanVehicleEntityBase Vehicle { get { return Entity as AckermanVehicleEntityBase; } }
    }
}