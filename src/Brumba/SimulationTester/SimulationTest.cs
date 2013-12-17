using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Mrse = Microsoft.Robotics.Simulation.Engine;
using MrsePxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.SimulationTester
{
    public interface ISimulationTest
    {
        void PrepareForReset(Mrse.VisualEntity entity);

        IEnumerator<ITask> Start();
        IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<MrsePxy.VisualEntity> simStateEntities, double elapsedTime);

        double EstimatedTime { get; }
        bool IsProbabilistic { get; }
        object Fixture { get; set; }
    }

    public abstract class SimulationTestBase : ISimulationTest
    {
        public object Fixture { get; set; }
        public double EstimatedTime { get; protected set; }

        public abstract bool IsProbabilistic { get; }

        public abstract void PrepareForReset(Mrse.VisualEntity entity);

        public abstract IEnumerator<ITask> Start();
        public abstract IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<MrsePxy.VisualEntity> simStateEntities, double elapsedTime);
    }

    public abstract class DeterministicTest : SimulationTestBase
    {
        //Test is deterministic, it is run once, no need to reload any entities. Besides, it may alleviate some problems (some joint locks, for example).
        public override void PrepareForReset(Mrse.VisualEntity entity)
        {
        }

        public override bool IsProbabilistic { get { return false; } }
    }

    public abstract class StochasticTest : SimulationTestBase
    {
        static readonly Random _randomG = new Random((int)DateTime.Now.Ticks);
        protected static Random RandomG { get { return _randomG; } }

        public override bool IsProbabilistic { get { return true; } }
    }
}


