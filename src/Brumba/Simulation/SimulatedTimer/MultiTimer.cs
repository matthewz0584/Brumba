using System;
using System.Collections.Generic;
using System.Linq;

namespace Brumba.Simulation.SimulatedTimer
{
    public class MultiTimer
    {
        readonly List<SubscriberState> _subscriberStates = new List<SubscriberState>();
        float _currentTime;
        
        public event Action<string, float> Tick = delegate {};

        public void Subscribe(string name, float interval)
        {
            if (_subscriberStates.Any(ss => ss.Name == name))
                throw new Exception(string.Format("Subscriber with name {0} is already registered", name));
            _subscriberStates.Add(new SubscriberState {Name = name, Interval = interval, LastTickTime = _currentTime});
        }

        public void Unsubscribe(string name)
        {
            _subscriberStates.Remove(_subscriberStates.Single(ss => ss.Name == name));
        }

        public void Update(float time)
        {
            _currentTime = time;
            foreach (var subscr in _subscriberStates.Where(subscr => (time - subscr.LastTickTime) > subscr.Interval))
            {
                Tick(subscr.Name, time);
                subscr.LastTickTime = time;
            }
        }

        public class SubscriberState
        {
            public string Name { get; set; }
            public float Interval { get; set; }
            public float LastTickTime { get; set; }
        }
    }
}