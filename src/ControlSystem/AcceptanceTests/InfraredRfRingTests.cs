using System;
using System.Collections.Generic;
using Brumba.SimulationTestRunner;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.AcceptanceTests
{
    [SimTestFixture("infrared_rf_ring")]
    public class InfraredRfRingTests
    {
        public Simulation.SimulatedInfraredRfRing.Proxy.SimulatedInfraredRfRingOperations IfRfRingPort { get; set; }

        [SetUp]
        public void SetUp(SimulationTestRunnerService testerService)
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

                @return(ringState.Distances[0].EqualsRelatively(0.3f, 0.05f) &&
                        ringState.Distances[1] == 1 &&
                        ringState.Distances[2].EqualsRelatively(0.1f, 0.05f) &&
                        ringState.Distances[3] == 1);
            }
        }
    }
}
