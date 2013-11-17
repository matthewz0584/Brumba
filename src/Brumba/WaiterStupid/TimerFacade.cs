using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brumba.WaiterStupid
{
    public interface ITimer
    {
        event Action<float> Tick;

        void Set(float interval);
    }

    public class TimerFacade : ITimer
    {
        public event Action<float> Tick = delegate { };

        public void Set(float interval)
        {}
    }
}
