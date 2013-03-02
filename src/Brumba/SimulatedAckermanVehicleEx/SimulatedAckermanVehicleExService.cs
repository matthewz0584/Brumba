using System.ComponentModel;
using Brumba.AckermanVehicle;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation.SimulatedAckermanVehicleEx
{
	[Contract(Contract.Identifier)]
	[DisplayName("Simulated Ackerman Vehicle Extended")]
	[Description("no description provided")]
	class SimulatedAckermanVehicleExService : DsspServiceBase
	{
		[ServiceState]
		private SimulatedAckermanVehicleExState _state = new SimulatedAckermanVehicleExState();

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = AckermanVehicle.Contract.Identifier)]
        private AckermanVehicleOperations _ackermanVehiclePort = new AckermanVehicleOperations();
        
        [ServicePort("/SimulatedAckermanVehicleEx", AllowMultipleInstances = true)]
		private SimulatedAckermanVehicleExOperations _mainPort = new SimulatedAckermanVehicleExOperations();

        private SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        private AckermanVehicleExEntity _vehicle;
		
		public SimulatedAckermanVehicleExService(DsspServiceCreationPort creationPort)
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
            LogInfo("SimulatedAckermanVehcileEx OnInsertEntity called");
            
            _vehicle = entity.Body as AckermanVehicleExEntity;
            _vehicle.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanVehicleEx OnDeleteEntity called");
            
            _vehicle = null;
            _state.Connected = false;

            SetUpForWaitingForEntity();
        }

        void OnUpdateDrivePower(UpdateDrivePower motorRequest)
        {
            _vehicle.SetDrivePower(motorRequest.Body.Value);

            motorRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
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

        void OnGet(Get getRequest)
        {
            DefaultGetHandler(getRequest);
        }

        void OnGet(AckermanVehicle.Get getRequest)
        {
            DefaultGetHandler(getRequest);
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
                        Arbiter.Receive<Break>(true, _ackermanVehiclePort, OnBreak)
                        ),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                        Arbiter.Receive<DsspDefaultLookup>(true, _ackermanVehiclePort, DefaultLookupHandler),
                        Arbiter.Receive<Get>(true, _mainPort, OnGet),
                        Arbiter.Receive<AckermanVehicle.Get>(true, _ackermanVehiclePort, OnGet)
                        )));
        }

	    private void ResetMainPortInterleave(Interleave ileave)
        {
            Activate(ileave);
            MainPortInterleave = ileave;
        }
	}
}