using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using System.ComponentModel;
using Microsoft.Ccr.Core;

namespace Brumba.Simulation.SimulatedTimer
{
    [Contract(Contract.Identifier)]
    [DisplayName("SimulatedTimerService")]
    [Description("SimulatedTimerService service (no description provided)")]
    class SimulatedTimerService : SimulatedEntityServiceBase
    {
        [ServiceState]
        private SimulatedTimerState _state = new SimulatedTimerState();

        [ServicePort("/SimulatedTimer", AllowMultipleInstances = true)]
        private SimulatedTimerOperations _mainPort = new SimulatedTimerOperations();

        public SimulatedTimerService(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
        {
        }

        protected override Interleave ConcreteWaitingInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)
                    ),
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler)
                    ));
        }

        protected override Interleave ConcreteActiveInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)
                    ),
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                    Arbiter.Receive<Get>(true, _mainPort, OnGet)
                    ));
        }

        protected override void OnInsertEntity()
        {
            LogInfo("SimulatedTimer OnInsertEntity called");
        }

        protected override void OnDeleteEntity()
        {
            LogInfo("SimulatedTimer OnDeleteEntity called");
            _state.ElapsedTime = 0;
            _state.StartTime = 0;
        }

        private void OnGet(Get getRequest)
        {
            if (Entity != null)
            {
                _state.ElapsedTime = (Entity as TimerEntity).ElapsedTime;
                _state.StartTime = (Entity as TimerEntity).StartTime;
            }
            DefaultGetHandler(getRequest);
        }
    }
}