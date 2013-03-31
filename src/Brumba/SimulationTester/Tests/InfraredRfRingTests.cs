using System;
using System.Collections.Generic;
using System.Linq;
using IrrPxy = Brumba.Simulation.SimulatedInfraredRfRing.Proxy;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.Simulation.SimulationTester.Tests
{
    [SimulationTestFixture]
    public class InfraredRfRingTests : SimulationTestFixture
    {
        public InfraredRfRingTests(ServiceForwarder serviceForwarder)
            : base(new ISimulationTest[] { new DistancesTest() }, "infrared_rf_ring", serviceForwarder)
        {
        }

        public IrrPxy.SimulatedInfraredRfRingOperations IfRfRingPort { get; set; }

        protected override void SetUpServicePorts(ServiceForwarder serviceForwarder)
        {
            IfRfRingPort = serviceForwarder.ForwardTo<IrrPxy.SimulatedInfraredRfRingOperations>("testee_rf_ring");
        }
    }

    public class DistancesTest : ISimulationTest
    {
        public IEnumerable<VisualEntity> FindEntitiesToRestore(IEnumerable<VisualEntity> entityPxies)
        {
            return entityPxies.Where(pxy => pxy.State.Name == "ring owner");
        }

        public IEnumerable<Microsoft.Robotics.Simulation.Engine.VisualEntity> FindAndPrepareEntitiesForRestore(IEnumerable<Microsoft.Robotics.Simulation.Engine.VisualEntity> entities)
        {
            return entities.Where(e => e.State.Name == "ring owner");
        }

        public IEnumerator<ITask> Start()
        {
            EstimatedTime = 1;
            yield break;
        }

        public IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
        {
            IrrPxy.SimulatedInfraredRfRingState ringState = null;
            yield return Arbiter.Receive<IrrPxy.SimulatedInfraredRfRingState>(false, (Fixture as InfraredRfRingTests).IfRfRingPort.Get(), rs => ringState = rs);

            @return(ringState.Distances[0] > 0.3 * 0.95 && ringState.Distances[0] < 0.3 * 1.05 &&
                    ringState.Distances[1] == 1 &&
                    ringState.Distances[2] > 0.1 * 0.95 && ringState.Distances[2] < 0.1 * 1.05 &&
                    ringState.Distances[3] == 1);
        }

        public double EstimatedTime { get; private set; }
        public ISimulationTestFixture Fixture { get; set; }
    }
}
