using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using SafwPxy = Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.Simulation.SimulationTester
{
    public interface ISimulationTest
    {
    	IEnumerable<EngPxy.VisualEntity> FindEntitiesToRestore(IEnumerable<EngPxy.VisualEntity> entityPxies);
    	IEnumerable<VisualEntity> PrepareEntitiesToRestore(IEnumerable<VisualEntity> entities);

        IEnumerator<ITask> Start(SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort);
        IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);

        double EstimatedTime { get; }
        ISimulationTestFixture Fixture { get; set; }
    }

    public abstract class SimulationTest : ISimulationTest
    {
        public double EstimatedTime { get; protected set; }
        public ISimulationTestFixture Fixture { get; set; }

		public IEnumerable<EngPxy.VisualEntity> FindEntitiesToRestore(IEnumerable<EngPxy.VisualEntity> entityPxies)
    	{
    		return entityPxies.Where(pxy => pxy.State.Name == "testee");
    	}

    	public IEnumerator<ITask> Start(SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            yield return To.Exec(Start, (double et) => EstimatedTime = et, vehiclePort);
        }

		public abstract IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);

        protected abstract IEnumerator<ITask> Start(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort);

		static Random _randomG = new Random((int)DateTime.Now.Ticks);
    	public IEnumerable<VisualEntity> PrepareEntitiesToRestore(IEnumerable<VisualEntity> entities)
    	{
    		var testee = entities.Single(e => e.State.Name == "testee");
			testee.State.Pose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, (float)(2 * Math.PI * _randomG.NextDouble()));

    		return new[] {testee};
    	}
    }
}


