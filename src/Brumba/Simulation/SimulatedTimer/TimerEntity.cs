using System;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Dss.Core.Attributes;

namespace Brumba.Simulation.SimulatedTimer
{
    [DataContract]
    public class TimerEntity : VisualEntity
    {
        public event Action<double> Tick = delegate {};
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
			//if (StartTime == 0d)
			//	StartTime = update.ApplicationTime;
			//else
			//{
			//	ElapsedTime = update.ApplicationTime - StartTime;
			//	Tick(ElapsedTime);
			//}
	        //ElapsedTime = update.ApplicationTime;
	        Time = update.ApplicationTime;
			update.ElapsedTime
			Tick(update.ApplicationTime);
        }
    }
}
