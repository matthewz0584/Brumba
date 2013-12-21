using System;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Dss.Core.Attributes;

namespace Brumba.Simulation.SimulatedTimer
{
    [DataContract]
    public class TimerEntity : VisualEntity
    {
        //delta, time
        public event Action<double, double> Tick = delegate {};
		public double Time { get; set; }

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
			Tick(update.ElapsedTime, update.ApplicationTime);
        }
    }
}
