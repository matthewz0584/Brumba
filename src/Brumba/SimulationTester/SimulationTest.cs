using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Engine;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.Simulation.SimulationTester
{
    public interface ISimulationTest
    {
    	bool NeedResetOnEachTry(EngPxy.VisualEntity entityProxy);
        void PrepareForReset(VisualEntity entity);

        IEnumerator<ITask> Start();
        IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);

        double EstimatedTime { get; }
        bool IsProbabilistic { get; }
        object Fixture { get; set; }
    }

    public abstract class SimulationTestBase : ISimulationTest
    {
        public object Fixture { get; set; }
        public double EstimatedTime { get; protected set; }

        public abstract bool IsProbabilistic { get; }

        public abstract bool NeedResetOnEachTry(EngPxy.VisualEntity entityProxy);
        public abstract void PrepareForReset(VisualEntity entity);

        public abstract IEnumerator<ITask> Start();
        public abstract IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);
    }

    public abstract class DeterministicTest : SimulationTestBase
    {
        //Test is deterministic, it is run once, no need to reload any entities. Besides, it may alleviate some problems (some joint locks, for example).
        public override bool NeedResetOnEachTry(EngPxy.VisualEntity entityProxy)
        {
            return false;
        }

        public override void PrepareForReset(VisualEntity entity)
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


