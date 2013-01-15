using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation.SimulatedStabilizer
{
	[Contract(Contract.Identifier)]
    [DisplayName("Simulated Stabilizer")]
    [Description("no description provided")]
	class SimulatedStabilizerService : DsspServiceBase
	{
		[ServiceState]
		SimulatedStabilizerState _state = new SimulatedStabilizerState();
		
		[ServicePort("/SimulatedStabilizer", AllowMultipleInstances = true)]
		SimulatedStabilizerOperations _mainPort = new SimulatedStabilizerOperations();

        SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        StabilizerEntity _stabilizer;

        public SimulatedStabilizerService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}

		protected override void Start()
		{
            SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _simEngineNotifyPort);

            base.Start();

            //Killing default main port interleave. From now on I control main port interleave
            MainPortInterleave.CombineWith(new Interleave(
                    new TeardownReceiverGroup(Arbiter.Receive<Park>(false, _mainPort, b => { })),
                    new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup()));
            _mainPort.Post(new Park());

            SetUpForWaitingForEntity(); 
		}

        void OnInsertEntity(InsertSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanFourWheels OnInsertEntity called");

            _stabilizer = entity.Body as StabilizerEntity;
            _stabilizer.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanFourWheels OnDeleteEntity called");

            _stabilizer = null;
            _state.Connected = false;

            SetUpForWaitingForEntity();
        }

        void OnGet(Get getRequest)
        {
            if (_stabilizer != null)
            {
                _state.LfWheelToGroundDistance = _stabilizer.LfWheelRf.Distance;
            }

            DefaultGetHandler(getRequest);
        }

        void OnMoveTail(MoveTail moveRequest)
        {
            moveRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnPark(Park parkRequest)
        {
            parkRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void SetUpForWaitingForEntity()
        {
            ResetMainPortInterleave(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                        Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)
                        ),
                    new ExclusiveReceiverGroup(),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                        Arbiter.Receive<Get>(true, _mainPort, OnGet)
                        )));
        }

        void SetUpForControlOfEntity()
        {
            ResetMainPortInterleave(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                        Arbiter.Receive<DeleteSimulationEntity>(false, _simEngineNotifyPort, OnDeleteEntity)
                        ),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<Park>(true, _mainPort, OnPark),
                        Arbiter.Receive<MoveTail>(true, _mainPort, OnMoveTail)
                        ),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                        Arbiter.Receive<Get>(true, _mainPort, OnGet)
                        )));
        }

        void ResetMainPortInterleave(Interleave ileave)
        {
            Activate(ileave);
            MainPortInterleave = ileave;
        }
	}
}