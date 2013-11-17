using System.Collections.Generic;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Services.SubscriptionManager;

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

        [SubscriptionManagerPartner("SubMgr")]
        private SubscriptionManagerPort _subMgrPort = new SubscriptionManagerPort();

        readonly MultiTimer _multiTimer = new MultiTimer();

        public SimulatedTimerService(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
        {
            _multiTimer.Tick += (subscr, time) => SendNotificationToTarget(subscr, _subMgrPort, new Update(new SimulatedTimerState { ElapsedTime = time }));
        }

        protected override Interleave ConcreteWaitingInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)
                    ),
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup()
                );
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
                    Arbiter.Receive<Get>(true, _mainPort, OnGet),
                    Arbiter.ReceiveWithIterator<Subscribe>(true, _mainPort, OnSubscribe)
                    ));
        }

        protected override void OnInsertEntity()
        {
            LogInfo("SimulatedTimer OnInsertEntity called");
            (Entity as TimerEntity).Tick += time => _multiTimer.Update((float)time);
        }

        protected override void OnDeleteEntity()
        {
            LogInfo("SimulatedTimer OnDeleteEntity called");
            _state.ElapsedTime = 0;
            _state.StartTime = 0;
        }

        void OnGet(Get getRequest)
        {
            _state.ElapsedTime = (Entity as TimerEntity).ElapsedTime;
            _state.StartTime = (Entity as TimerEntity).StartTime;
            DefaultGetHandler(getRequest);
        }

        IEnumerator<ITask> OnSubscribe(Subscribe subscribeRq)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribeRq.Body, subscribeRq.ResponsePort),
                success => _multiTimer.Subscribe(subscribeRq.Body.Subscriber, subscribeRq.Body.Interval),
                LogError);
        }
    }
}