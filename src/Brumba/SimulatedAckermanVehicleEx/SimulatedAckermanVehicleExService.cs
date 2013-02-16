using System.ComponentModel;
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
                    new TeardownReceiverGroup(Arbiter.Receive<Break>(false, _mainPort, b => { })),
                    new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup()));
            _mainPort.Post(new Break());

            SetUpForWaitingForEntity(); 
		}

        private void OnInsertEntity(InsertSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanVehcileEx OnInsertEntity called");
            
            _vehicle = entity.Body as AckermanVehicleExEntity;
            _vehicle.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        private void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanVehicleEx OnDeleteEntity called");
            
            _vehicle = null;
            _state.Connected = false;
            _state.MotorPower = 0;
            _state.SteerAngle = 0;

            SetUpForWaitingForEntity();
        }

        private void OnSetMotorPower(SetMotorPower motorRequest)
        {
            _state.MotorPower = motorRequest.Body.Value;
            _vehicle.SetMotorPower(_state.MotorPower);

            motorRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        private void OnSetSteerAngle(SetSteerAngle steerRequest)
        {
            _state.SteerAngle = steerRequest.Body.Value;
            _vehicle.SetSteerAngle(_state.SteerAngle);

            steerRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        private void OnBreak(Break breakRequest)
        {
            _state.MotorPower = 0;
            _vehicle.Break();

            breakRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        private void OnGet(Get getRequest)
        {
            _state.Velocity = _vehicle.Velocity;
            _state.ActualSteerAngle = _vehicle.SteerAngle;
            DefaultGetHandler(getRequest);
        }

        private void SetUpForWaitingForEntity()
        {
            ResetMainPortInterleave(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                        Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)
                        ),
                    new ExclusiveReceiverGroup(),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                        Arbiter.Receive<Get>(true, _mainPort, DefaultGetHandler)
                        )));
        }

        private void SetUpForControlOfEntity()
        {
            ResetMainPortInterleave(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                        Arbiter.Receive<DeleteSimulationEntity>(false, _simEngineNotifyPort, OnDeleteEntity)
                        ),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<SetMotorPower>(true, _mainPort, OnSetMotorPower),
                        Arbiter.Receive<SetSteerAngle>(true, _mainPort, OnSetSteerAngle),
                        Arbiter.Receive<Break>(true, _mainPort, OnBreak)
                        ),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                        Arbiter.Receive<Get>(true, _mainPort, OnGet)
                        )));
        }

	    private void ResetMainPortInterleave(Interleave ileave)
        {
            Activate(ileave);
            MainPortInterleave = ileave;
        }
	}
}