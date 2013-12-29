using System.Collections.Generic;
using System.Linq;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Services.SubscriptionManager;
using Microsoft.Robotics.Simulation.Engine;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedTimer
{
    [Contract(Contract.Identifier)]
    [DisplayName("SimulatedTimerService")]
    [Description("SimulatedTimerService service (no description provided)")]
    class SimulatedTimerService : SimulatedEntityServiceBase
    {
        public static float GetElapsedTime(FrameUpdate frameUpdate)
        {
            if (Instance == null || Instance.TimerEntity == null || !Instance.TimerEntity.Paused)
                return (float) frameUpdate.ElapsedTime;
            return 0;
        }

        static SimulatedTimerService Instance;

        [ServiceState]
        readonly SimulatedTimerState _state = new SimulatedTimerState();

        [ServicePort("/SimulatedTimer", AllowMultipleInstances = true)]
        SimulatedTimerOperations _mainPort = new SimulatedTimerOperations();

        [SubscriptionManagerPartner("SubMgr")]
        SubscriptionManagerPort _subMgrPort = new SubscriptionManagerPort();

        readonly MultiTimer _multiTimer = new MultiTimer();

        Port<Tick> _multiTimerUpdatePort = new Port<Tick>();

        public SimulatedTimerService(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
        {
            Instance = this;
            _multiTimer.Tick += (subscr, dt, t) => 
                    SendNotificationToTarget(subscr, _subMgrPort, new Update(new SimulatedTimerState { Time = t, Delta = dt }));
        }

        protected override void Start()
        {
            base.Start();

            MainPortInterleave.CombineWith(
                new Interleave(
                    new ExclusiveReceiverGroup(),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive(true, _multiTimerUpdatePort, tick => _multiTimer.Update(tick.Dt, tick.T)))
                        ));
        }

        protected override void OnInsertEntity()
        {
            TimerEntity.Tick += OnTimerEntityTick;
        }

        protected override void OnDeleteEntity()
        {
            TimerEntity.Tick -= OnTimerEntityTick;

            _state.Time = 0;

            var subscrMgrGetResponse = new PortSet<SubscriptionListType, Fault>();
            _subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Get { ResponsePort = subscrMgrGetResponse });
            //Well-behaved services will unsubscribe from notifications on drop down via subscription shutdown port. I will never know about it - there is no way to do it.
            //In simulation tester environment every service with timer subscription will be dead for sure by the moment of environment restoration. So there should be no alive subscriptions for it.
            //Synchronize multitimer subscriptions with subscription manager subscriptions. To save resources.
            //POSSIBLE PROBLEM - asynchronous call, no guarantee that it will be executed to the end by the moment of next OnInsertEntity - but it's very unlikely
            MainPortInterleave.CombineWith(new Interleave(
                    new ExclusiveReceiverGroup(
                        subscrMgrGetResponse.Receive(subMgrState => _multiTimer.Reset(subMgrState.Subscription.Select(st => st.Subscriber).ToArray()))),
                    new ConcurrentReceiverGroup()
                        ));
        }

        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public void OnGet(Get getRequest)
        {
			if (IsConnected)
				_state.Time = TimerEntity.Time;
            DefaultGetHandler(getRequest);
        }

	    [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public IEnumerator<ITask> OnSubscribe(Subscribe subscribeRq)
        {
            yield return SubscribeHelper(_subMgrPort, subscribeRq.Body, subscribeRq.ResponsePort).Choice(
                success => _multiTimer.Subscribe(subscribeRq.Body.Subscriber, subscribeRq.Body.Interval),
                LogError);
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnPause(Pause pauseRq)
        {
            if (FaultIfNotConnected(pauseRq))
                return;

            TimerEntity.Paused = pauseRq.Body.Pause;
            pauseRq.ResponsePort.Post(new DefaultUpdateResponseType());
        }

		protected override ISimulationEntityServiceState GetState() { return _state; }

        void OnTimerEntityTick(double dt, double t)
        {
            _multiTimerUpdatePort.Post(new Tick { Dt = (float)dt, T = (float)t });
        }

		TimerEntity TimerEntity { get { return Entity as TimerEntity; } }

        class Tick
        {
            public float Dt { get; set; }
            public float T { get; set; }
        }
    }
}