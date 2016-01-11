using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Mrse = Microsoft.Robotics.Simulation.Engine;
using MrsePxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.SimulationTestRunner
{
    public class SimulationTestInfo
    {
        public string Name { get; set; }

        public object Object { get; set; }

        public float EstimatedTime { get; set; }

        public bool IsProbabilistic { get; set; }

		public bool TestAllEntities { get; set; }

        public Action<IEnumerable<Mrse.VisualEntity>> Prepare { get; set; }

        public Func<IEnumerator<ITask>> Start { get; set; }

        public Func<Action<bool>, IEnumerable<MrsePxy.VisualEntity>, double, IEnumerator<ITask>> Test { get; set; }
    }
}