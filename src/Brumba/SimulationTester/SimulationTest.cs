using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Engine;
using SafwPxy = Brumba.Simulation.SimulatedAckermanVehicle.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.Simulation.SimulationTester
{
    public interface ISimulationTest
    {
    	IEnumerable<EngPxy.VisualEntity> FindEntitiesToRestore(IEnumerable<EngPxy.VisualEntity> entityPxies);
    	IEnumerable<VisualEntity> FindAndPrepareEntitiesForRestore(IEnumerable<VisualEntity> entities);

        IEnumerator<ITask> Start();
        IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);

        double EstimatedTime { get; }
        bool IsProbabilistic { get; }
        ISimulationTestFixture Fixture { get; set; }
    }

    public abstract class SimulationTestBase : ISimulationTest
    {
        public ISimulationTestFixture Fixture { get; set; }
        public double EstimatedTime { get; protected set; }

        public abstract bool IsProbabilistic { get; }
        public abstract IEnumerable<EngPxy.VisualEntity> FindEntitiesToRestore(IEnumerable<EngPxy.VisualEntity> entityPxies);
        public abstract IEnumerable<VisualEntity> FindAndPrepareEntitiesForRestore(IEnumerable<VisualEntity> entities);
        public abstract IEnumerator<ITask> Start();
        public abstract IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);
    }

    public abstract class DeterministicTestBase : SimulationTestBase
    {
        //Test is determinixtic, it is run once, no need reload any entities. Besides, it may alleviate some problems (some joint locks, for example)
        public override IEnumerable<EngPxy.VisualEntity> FindEntitiesToRestore(IEnumerable<EngPxy.VisualEntity> entityPxies)
        {
            return new EngPxy.VisualEntity[] {};
        }

        public override IEnumerable<VisualEntity> FindAndPrepareEntitiesForRestore(IEnumerable<VisualEntity> entities)
        {
            return new VisualEntity[] {};
        }

        public override bool IsProbabilistic { get { return false; } }
    }

    public abstract class StochasticTestBase : SimulationTestBase
    {
        static readonly Random _randomG = new Random((int)DateTime.Now.Ticks);
        protected static Random RandomG { get { return _randomG; } }

        public override bool IsProbabilistic { get { return true; } }
    }
}


