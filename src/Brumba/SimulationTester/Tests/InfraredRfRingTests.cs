using System;
using System.Collections.Generic;
using IrrPxy = Brumba.Simulation.SimulatedInfraredRfRing.Proxy;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.Simulation.SimulationTester.Tests
{
    [SimTestFixture("infrared_rf_ring")]
    public class InfraredRfRingTests
    {
        public IrrPxy.SimulatedInfraredRfRingOperations IfRfRingPort { get; set; }

        [SimSetUp]
        public void SetUp(ServiceForwarder serviceForwarder)
        {
            IfRfRingPort = serviceForwarder.ForwardTo<IrrPxy.SimulatedInfraredRfRingOperations>("testee_rf_ring");
        }

        [SimTest]
        public class DistancesTest : DeterministicTestBase
        {
            public override IEnumerator<ITask> Start()
            {
                EstimatedTime = 1;
                yield break;
            }

            public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
            {
                IrrPxy.SimulatedInfraredRfRingState ringState = null;
                yield return Arbiter.Receive<IrrPxy.SimulatedInfraredRfRingState>(false, (Fixture as InfraredRfRingTests).IfRfRingPort.Get(), rs => ringState = rs);

                @return(ringState.Distances[0] > 0.3 * 0.95 && ringState.Distances[0] < 0.3 * 1.05 &&
                        ringState.Distances[1] == 1 &&
                        ringState.Distances[2] > 0.1 * 0.95 && ringState.Distances[2] < 0.1 * 1.05 &&
                        ringState.Distances[3] == 1);
            }
        }
    }
}
