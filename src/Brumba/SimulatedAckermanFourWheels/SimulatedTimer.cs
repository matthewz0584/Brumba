using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Dss.ServiceModel.Dssp;
using System.ComponentModel;
using Microsoft.Ccr.Core;

namespace Brumba.Simulation.SimulatedTimer
{
    [Contract(Contract.Identifier)]
    [DisplayName("SimulatedTimerService")]
    [Description("SimulatedTimerService service (no description provided)")]
    class SimulatedTimerService : DsspServiceBase
    {
        [ServiceState]
        private SimulatedTimerState _state = new SimulatedTimerState();

        [ServicePort("/SimulatedTimer", AllowMultipleInstances = true)]
        private SimulatedTimerOperations _mainPort = new SimulatedTimerOperations();

        private SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        private TimerEntity _timer;

        public SimulatedTimerService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        protected override void Start()
        {
            SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _simEngineNotifyPort);

            base.Start();

            //Killing default main port interleave. From now on I control main port interleave
            MainPortInterleave.CombineWith(new Interleave(
                    new TeardownReceiverGroup(Arbiter.Receive<FirstInterleaveShutdown>(false, _mainPort, shutdown => { })),
                    new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup()));
            _mainPort.Post(new FirstInterleaveShutdown());

            SetUpForWaitingForEntity();
        }

        private void OnInsertEntity(InsertSimulationEntity entity)
        {
            LogInfo("SimulatedTimer OnInsertEntity called");

            _timer = entity.Body as TimerEntity;
            _timer.ServiceContract = Contract.Identifier;

            SetUpForControlOfEntity();
        }

        private void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedTimer OnDeleteEntity called");

            _timer = null;
            _state.ElapsedTime = 0;
            _state.StartTime = 0;

            SetUpForWaitingForEntity();
        }

        private void OnGet(Get getRequest)
        {
            if (_timer != null)
            {
                _state.ElapsedTime = _timer.ElapsedTime;
                _state.StartTime = _timer.StartTime;
            }
            base.DefaultGetHandler(getRequest);
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
                        Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler)
                        )));
        }

        private void SetUpForControlOfEntity()
        {
            ResetMainPortInterleave(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                        Arbiter.Receive<DeleteSimulationEntity>(false, _simEngineNotifyPort, OnDeleteEntity)
                        ),
                    new ExclusiveReceiverGroup(),
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