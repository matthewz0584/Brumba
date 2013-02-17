using System.ComponentModel;
using Brumba.AckermanVehicle;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using Break = Brumba.AckermanVehicle.Break;

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
        private AckermanVehicleEntity _vehicle;

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

            _vehicle = entity.Body as AckermanVehicleEntity;
            _vehicle.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanVehicleEx OnDeleteEntity called");

            _vehicle = null;
            _state.Connected = false;
            _state.DriveMotorTicks = 0;
            _state.SteerMotorTicks = 0;

            SetUpForWaitingForEntity();
        }

        void OnUpdateDrivePower(UpdateDrivePower powerRequest)
        {
            _vehicle.SetDrivePower(powerRequest.Body.Value);

            powerRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnUpdateSteerAngle(UpdateSteerAngle steerRequest)
        {
            _vehicle.SetSteerAngle(steerRequest.Body.Value);

            steerRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnBreak(Break breakRequest)
        {
            _vehicle.Break();

            breakRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnReplace(AckermanVehicle.Replace replaceRequest)
        {
            _state.TicksPerDriveAngleRadian = replaceRequest.Body.TicksPerDriveAngleRadian;
            _state.TicksPerSteeringAngleRadian = replaceRequest.Body.TicksPerSteeringAngleRadian;

            replaceRequest.ResponsePort.Post(DefaultReplaceResponseType.Instance);
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
                                            Arbiter.Receive<UpdateSteerAngle>(true, _ackermanVehiclePort, OnUpdateSteerAngle),
                                            Arbiter.Receive<Break>(true, _ackermanVehiclePort, OnBreak),
                                            Arbiter.Receive<AckermanVehicle.Replace>(true, _ackermanVehiclePort, OnReplace)
                                            ),
                                        new ConcurrentReceiverGroup(
                                            Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                                            Arbiter.Receive<DsspDefaultLookup>(true, _ackermanVehiclePort, DefaultLookupHandler),
                                            Arbiter.Receive<Get>(true, _mainPort, DefaultGetHandler),
                                            Arbiter.Receive<AckermanVehicle.Get>(true, _ackermanVehiclePort, DefaultGetHandler)
                                            )));
        }

        void ResetMainPortInterleave(Interleave ileave)
        {
            Activate(ileave);
            MainPortInterleave = ileave;
        }
    }
}