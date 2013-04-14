using System.ComponentModel;
using Brumba.AckermanVehicle;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    [Contract(Contract.Identifier)]
    [DisplayName("Simulated Ackerman Vehicle")]
    [Description("no description provided")]
    class SimulatedAckermanVehicleService : DsspServiceBase
    {
        [ServiceState]
        private SimulatedAckermanVehicleState _state = new SimulatedAckermanVehicleState();

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = AckermanVehicle.Contract.Identifier)]
        private AckermanVehicleOperations _ackermanVehiclePort = new AckermanVehicleOperations();

        [ServicePort("/SimulatedAckermanVehicle", AllowMultipleInstances = true)]
        private SimulatedAckermanVehicleOperations _mainPort = new SimulatedAckermanVehicleOperations();

        private SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        private AckermanVehicleEntityBase _vehicle;

        public SimulatedAckermanVehicleService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        protected override void Start()
        {
            SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _simEngineNotifyPort);

            base.Start();

            //Killing default main port interleave. From now on I control main port interleave
            MainPortInterleave.CombineWith(new Interleave(
                    new TeardownReceiverGroup(Arbiter.Receive<Break>(false, _ackermanVehiclePort, b => { })),
                    new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup()));
            _ackermanVehiclePort.Post(new Break());

            SetUpForWaitingForEntity();
        }

        void OnInsertEntity(InsertSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanVehcile OnInsertEntity called");

            _vehicle = entity.Body as AckermanVehicleEntityBase;
            _vehicle.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanVehicle OnDeleteEntity called");

            _vehicle = null;
            _state.Connected = false;
            _state.DriveAngularDistance = 0;
            _state.SteeringAngle = 0;

            SetUpForWaitingForEntity();
        }

        void OnUpdateDrivePower(UpdateDrivePower powerRequest)
        {
            _vehicle.SetDrivePower(powerRequest.Body.Value);

            powerRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnUpdateSteeringAngle(UpdateSteeringAngle steeringRequest)
        {
            _vehicle.SetSteeringAngle(steeringRequest.Body.Value);

            steeringRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnBreak(Break breakRequest)
        {
            _vehicle.Break();

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

        void SetUpForWaitingForEntity()
        {
            ResetMainPortInterleave(new Interleave(
                                        new TeardownReceiverGroup(
                                            Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                                            Arbiter.Receive<DsspDefaultDrop>(false, _ackermanVehiclePort, DefaultDropHandler),
                                            Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)
                                            ),
                                        new ExclusiveReceiverGroup(),
                                        new ConcurrentReceiverGroup(
                                            Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                                            Arbiter.Receive<DsspDefaultLookup>(true, _ackermanVehiclePort, DefaultLookupHandler),
                                            Arbiter.Receive<Get>(true, _mainPort, DefaultGetHandler)
                                            )));
        }

        void SetUpForControlOfEntity()
        {
            ResetMainPortInterleave(new Interleave(
                                        new TeardownReceiverGroup(
                                            Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                                            Arbiter.Receive<DsspDefaultDrop>(false, _ackermanVehiclePort, DefaultDropHandler),
                                            Arbiter.Receive<DeleteSimulationEntity>(false, _simEngineNotifyPort, OnDeleteEntity)
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
                                            )));
        }

        void UpdateState()
        {
            _state.DriveAngularDistance = _vehicle.GetDriveAngularDistance();
            _state.SteeringAngle = _vehicle.GetSteeringAngle();
        }

        void ResetMainPortInterleave(Interleave ileave)
        {
            Activate(ileave);
            MainPortInterleave = ileave;
        }
    }
}