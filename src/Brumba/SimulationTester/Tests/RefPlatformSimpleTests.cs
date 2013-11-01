using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using MrsdPxy = Microsoft.Robotics.Services.Drive.Proxy;
using VisualEntityPxy = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;

namespace Brumba.Simulation.SimulationTester.Tests
{
    [SimTestFixture("ref_platform_simple_tests", Wip = true)]
    public class RefPlatformSimpleTests
    {
		public MrsdPxy.DriveOperations RefPlDrivePort { get; set; }

        [SimSetUp]
        public void SetUp(ServiceForwarder serviceForwarder)
        {
			RefPlDrivePort = serviceForwarder.ForwardTo<MrsdPxy.DriveOperations>("stupid_waiter_ref_platform/simulateddifferentialdrive");
        }

        [SimTest]
        public class DriveForwardTest : DeterministicTestBase
        {
            public override IEnumerator<ITask> Start()
            {
                EstimatedTime = 10;
	            (Fixture as RefPlatformSimpleTests).RefPlDrivePort.DriveDistance(10, 0.9);
                yield break;
            }

            public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<VisualEntityPxy> simStateEntities, double elapsedTime)
            {
                @return(false);
                yield break;
            }
        }
    }
}
