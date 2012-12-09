using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using SafwPxy = Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.Simulation.SimulationTester
{
    public interface ISimulationTest
    {
        IEnumerator<ITask> Start(SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort);
        IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);
        double EstimatedTime { get; }
        ISimulationTestFixture Fixture { get; set; }
    }

    public abstract class SimulationTest : ISimulationTest
    {
        public double EstimatedTime { get; protected set; }
        public ISimulationTestFixture Fixture { get; set; }

        public IEnumerator<ITask> Start(SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            yield return To.Exec(Start, (double et) => EstimatedTime = et, vehiclePort);
        }

        public abstract IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);

        protected abstract IEnumerator<ITask> Start(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort);
    }
}


