﻿using System;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Dss.Core.Attributes;

namespace Brumba.Simulation.SimulatedTimer
{
    [DataContract]
    public class TimerEntity : VisualEntity
    {
        public event Action<double> Tick = delegate {};
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
            Tick(ElapsedTime);
        }
    }
}
