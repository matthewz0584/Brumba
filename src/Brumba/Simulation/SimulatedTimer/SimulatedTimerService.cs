﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Services.SubscriptionManager;
using W3C.Soap;

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
            _multiTimer.Tick += (subscr, time) => 
                    SendNotificationToTarget(subscr, _subMgrPort, new Update(new SimulatedTimerState { ElapsedTime = time }));
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
            (Entity as TimerEntity).Tick += time => _multiTimer.Update((float)time);
        }

        protected override void OnDeleteEntity()
        {
            _state.ElapsedTime = 0;
            _state.StartTime = 0;

            var subscrMgrGetResponse = new PortSet<SubscriptionListType, Fault>();
            _subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Get { ResponsePort = subscrMgrGetResponse });
            //To keep on the safe side reset all internal timers for all registered subscribers right now
            _multiTimer.Reset(_multiTimer.Subscribers);
            Activate(subscrMgrGetResponse.Choice(
                //Well-behaved services will unsubscribe from notifications on drop down via subscription shutdown port. I will never know about it - there is no way to do it.
                //In simulation tester environment every service with timer subscription will be dead for sure by the moment of environment restoration. So there should be no alive subscriptions for it.
                //Synchronize multitimer subscriptions with subscription manager subscriptions. To save resources.
                //POSSIBLE PROBLEM - asynchronous call, no guarantee that it will be executed to the end by the moment of next OnInsertEntity - but it's very unlikely
                subMgrState => 
					_multiTimer.Reset(subMgrState.Subscription.Select(st => st.Subscriber).ToArray()),
                LogError));
        }

        void OnGet(Get getRequest)
        {
            _state.ElapsedTime = (Entity as TimerEntity).ElapsedTime;
            _state.StartTime = (Entity as TimerEntity).StartTime;
			_state.Connected = Connected;
            DefaultGetHandler(getRequest);
        }

        IEnumerator<ITask> OnSubscribe(Subscribe subscribeRq)
        {
            yield return SubscribeHelper(_subMgrPort, subscribeRq.Body, subscribeRq.ResponsePort).Choice(
                success => _multiTimer.Subscribe(subscribeRq.Body.Subscriber, subscribeRq.Body.Interval),
                LogError);
        }
    }
}