using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using drivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using sickPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using VisualEntityPxy = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;
using Quaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.Simulation.SimulationTester.Tests
{
    [SimTestFixture("ref_platform_simple_tests", Wip = true)]
    public class RefPlatformSimpleTests
    {
		public drivePxy.DriveOperations RefPlDrivePort { get; set; }
		public sickPxy.SickLRFOperations SickLrfPort { get; set; }

        [SimSetUp]
        public void SetUp(ServiceForwarder serviceForwarder)
        {
			RefPlDrivePort = serviceForwarder.ForwardTo<drivePxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			SickLrfPort = serviceForwarder.ForwardTo<sickPxy.SickLRFOperations>("stupid_waiter_lidar/sicklrf");
        }

        //[SimTest]
        public class DriveForwardTest : DeterministicTest
        {
	        public override IEnumerator<ITask> Start()
            {
				//Max speed = 1,6 m/s, distance 2 meters
				EstimatedTime = 2 * 2 / 1.6;

		        (Fixture as RefPlatformSimpleTests).RefPlDrivePort.EnableDrive(true);
	            (Fixture as RefPlatformSimpleTests).RefPlDrivePort.SetDrivePower(1.0, 1.0);
                yield break;
            }

			public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(simStateEntities.Single(epxy => epxy.State.Name == "stupid_waiter").State.Pose.Position));
				@return(pos.Length() > 2);
				yield break;
			}
        }

		[SimTest]
		public class LrfTest : DeterministicTest
		{
			public override IEnumerator<ITask> Start()
			{
				EstimatedTime = 1;
				yield break;
			}

			public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				sickPxy.State lrfState = null;
				yield return Arbiter.Receive<sickPxy.State>(false, (Fixture as RefPlatformSimpleTests).SickLrfPort.Get(), ss => lrfState = ss);

				if (lrfState.DistanceMeasurements == null)
				{
					@return(false);
					yield break;
				}

				@return(lrfState.DistanceMeasurements.Length == 667 &&
						lrfState.DistanceMeasurements.Last() == 4500 &&
						lrfState.DistanceMeasurements.Skip(10).Take(667 - 10).All(d => d == 5600));
			}
		}
    }
}
