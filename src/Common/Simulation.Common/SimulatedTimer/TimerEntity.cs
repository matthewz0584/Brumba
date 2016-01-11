using System;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation.Common.SimulatedTimer
{
    [DataContract]
    public class TimerEntity : VisualEntity
    {
        //delta, time
        public event Action<double, double> Tick = delegate {};
		
        public double Time { get; set; }

        public bool Paused { get; set; }

        public TimerEntity()
        {
        }

        public TimerEntity(string name)
        {
            State.Name = name;
        }

        public override void Update(FrameUpdate update)
        {
            base.Update(update);
	        Time = update.ApplicationTime;
            if (!Paused)
			    Tick(update.ElapsedTime, update.ApplicationTime);
        }
    }
}
