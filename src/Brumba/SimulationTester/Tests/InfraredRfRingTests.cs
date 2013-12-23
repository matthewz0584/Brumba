using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.SimulationTester.Tests
{
    [SimTestFixture("infrared_rf_ring")]
    public class InfraredRfRingTests
    {
        public Simulation.SimulatedInfraredRfRing.Proxy.SimulatedInfraredRfRingOperations IfRfRingPort { get; set; }

        [SetUp]
		public void SetUp(SimulationTesterService testerService)
        {
            IfRfRingPort = testerService.ForwardTo<Simulation.SimulatedInfraredRfRing.Proxy.SimulatedInfraredRfRingOperations>("testee_rf_ring");
        }

        [SimTest(1, IsProbabilistic = false)]
        public class DistancesTest
        {
            [Fixture]
            public InfraredRfRingTests Fixture { get; set; }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield break;
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
            {
                Simulation.SimulatedInfraredRfRing.Proxy.SimulatedInfraredRfRingState ringState = null;
                yield return Arbiter.Receive<Simulation.SimulatedInfraredRfRing.Proxy.SimulatedInfraredRfRingState>(false, Fixture.IfRfRingPort.Get(), rs => ringState = rs);

                @return(ringState.Distances[0] > 0.3 * 0.95 && ringState.Distances[0] < 0.3 * 1.05 &&
                        ringState.Distances[1] == 1 &&
                        ringState.Distances[2] > 0.1 * 0.95 && ringState.Distances[2] < 0.1 * 1.05 &&
                        ringState.Distances[3] == 1);
            }
        }
    }
}
