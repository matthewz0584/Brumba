using System;
using System.Collections.Generic;
using System.Linq;
using DC = System.Diagnostics.Contracts;

namespace Brumba.Simulation.Common.SimulatedTimer
{
    public class MultiTimer
    {
        public class SubscriberState
        {
            public string Name { get; set; }
            public float Interval { get; set; }
            public float ElapsedTime { get; set; }
        }

        readonly List<SubscriberState> _subscriberStates = new List<SubscriberState>();
        
        //subsriber name, delta, time
        public event Action<string, float, float> Tick = delegate {};

        public void Subscribe(string name, float interval)
        {
            if (_subscriberStates.Any(ss => ss.Name == name))
                throw new Exception(string.Format("Subscriber with name {0} is already registered", name));
            _subscriberStates.Add(new SubscriberState {Name = name, Interval = interval, ElapsedTime = 0});
        }

        public string[] Subscribers
        {
            get { return _subscriberStates.Select(ss => ss.Name).ToArray(); }
        }

        public void Unsubscribe(string name)
        {
            _subscriberStates.Remove(_subscriberStates.Single(ss => ss.Name == name));
        }

        public void Update(float dt, float t)
        {
            foreach (var subscr in _subscriberStates)
                subscr.ElapsedTime += dt;

            foreach (var subscr in _subscriberStates.Where(subscr => subscr.ElapsedTime > subscr.Interval))
            {
                DC.Contract.Assert(subscr.ElapsedTime > 0);
                Tick(subscr.Name, subscr.ElapsedTime, t);
                subscr.ElapsedTime = 0;
            }
        }

        public void Reset(string[] survivedSubscribers)
        {
            foreach (var removedSubscriber in Subscribers.Except(survivedSubscribers))
                Unsubscribe(removedSubscriber);
            foreach (var subscr in _subscriberStates)
                subscr.ElapsedTime = 0;
        }
    }
}