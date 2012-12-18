using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Dss.Core.Attributes;

namespace Brumba.Simulation.SimulatedTimer
{
    [DataContract]
    public class TimerEntity : VisualEntity
    {
        public double StartTime { get; private set; }
        public double ElapsedTime { get; private set; }

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
            if (StartTime == 0)
                StartTime = update.ApplicationTime;
            ElapsedTime = update.ApplicationTime - StartTime;
        }
    }
}
