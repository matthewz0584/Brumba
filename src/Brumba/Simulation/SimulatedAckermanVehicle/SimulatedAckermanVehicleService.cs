using System.ComponentModel;
using Brumba.AckermanVehicle;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    [Contract(Contract.Identifier)]
    [DisplayName("Simulated Ackerman Vehicle")]
    [Description("no description provided")]
    class SimulatedAckermanVehicleService : SimulatedEntityServiceBase
    {
        [ServiceState]
        private SimulatedAckermanVehicleState _state = new SimulatedAckermanVehicleState();

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = AckermanVehicle.Contract.Identifier)]
        private AckermanVehicleOperations _ackermanVehiclePort = new AckermanVehicleOperations();

        [ServicePort("/SimulatedAckermanVehicle", AllowMultipleInstances = true)]
        private SimulatedAckermanVehicleOperations _mainPort = new SimulatedAckermanVehicleOperations();

        public SimulatedAckermanVehicleService(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
        {
        }

        protected override Interleave ConcreteWaitingInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                    Arbiter.Receive<DsspDefaultDrop>(false, _ackermanVehiclePort, DefaultDropHandler)
                    ),
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                    Arbiter.Receive<DsspDefaultLookup>(true, _ackermanVehiclePort, DefaultLookupHandler),
                    Arbiter.Receive<Get>(true, _mainPort, DefaultGetHandler)
                    ));
        }

        protected override Interleave ConcreteActiveInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                    Arbiter.Receive<DsspDefaultDrop>(false, _ackermanVehiclePort, DefaultDropHandler)
                    ),
                new ExclusiveReceiverGroup(
                    Arbiter.Receive<UpdateDrivePower>(true, _ackermanVehiclePort, OnUpdateDrivePower),
                    Arbiter.Receive<UpdateSteeringAngle>(true, _ackermanVehiclePort, OnUpdateSteeringAngle),
                    Arbiter.Receive<Break>(true, _ackermanVehiclePort, OnBreak)
                    ),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                    Arbiter.Receive<DsspDefaultLookup>(true, _ackermanVehiclePort, DefaultLookupHandler),
                    Arbiter.Receive<Get>(true, _mainPort, OnGet),
                    Arbiter.Receive<AckermanVehicle.Get>(true, _ackermanVehiclePort, OnGet)
                    ));
        }

        protected override void OnInsertEntity()
        {
            LogInfo("SimulatedAckermanVehcile OnInsertEntity called");
            _state.Connected = true;
        }

        protected override void OnDeleteEntity()
        {
            LogInfo("SimulatedAckermanVehicle OnDeleteEntity called");
            _state.Connected = false;
            _state.DriveAngularDistance = 0;
            _state.SteeringAngle = 0;
        }

        void OnUpdateDrivePower(UpdateDrivePower powerRequest)
        {
            Vehicle.SetDrivePower(powerRequest.Body.Value);

            powerRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnUpdateSteeringAngle(UpdateSteeringAngle steeringRequest)
        {
            Vehicle.SetSteeringAngle(steeringRequest.Body.Value);

            steeringRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnBreak(Break breakRequest)
        {
            Vehicle.Break();

            breakRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnGet(AckermanVehicle.Get get)
        {
            UpdateState();

            DefaultGetHandler(get);
        }

        void OnGet(Get get)
        {
            UpdateState();

            DefaultGetHandler(get);
        }

        void UpdateState()
        {
            _state.DriveAngularDistance = Vehicle.GetDriveAngularDistance();
            _state.SteeringAngle = Vehicle.GetSteeringAngle();
        }

        AckermanVehicleEntityBase Vehicle { get { return Entity as AckermanVehicleEntityBase; } }
    }
}