using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
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
}


