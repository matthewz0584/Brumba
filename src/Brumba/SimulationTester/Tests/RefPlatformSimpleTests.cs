using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using MrsdPxy = Microsoft.Robotics.Services.Drive.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using VisualEntityPxy = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;
using Quaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.Simulation.SimulationTester.Tests
{
    [SimTestFixture("ref_platform_simple_tests", Wip = true)]
    public class RefPlatformSimpleTests
    {
		public MrsdPxy.DriveOperations RefPlDrivePort { get; set; }

        [SimSetUp]
        public void SetUp(ServiceForwarder serviceForwarder)
        {
			RefPlDrivePort = serviceForwarder.ForwardTo<MrsdPxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
        }

        [SimTest]
        public class DriveForwardTest : StochasticTestBase
        {
			public override bool NeedResetOnEachTry(EngPxy.VisualEntity entityProxy)
			{
				return entityProxy.State.Name == "stupid_waiter";
			}

			public override void PrepareForReset(VisualEntity entity)
			{
				entity.State.Pose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, (float)(2 * Math.PI * RandomG.NextDouble()));
			}

	        public override IEnumerator<ITask> Start()
            {
				//float motorPower = 0.6f;
				//EstimatedTime = 50 / (AckermanVehicles.HardRearDriven.MaxVelocity * motorPower);//50 meters

                EstimatedTime = 2;//investigate real parameters of Eddie and SimEntity
		        (Fixture as RefPlatformSimpleTests).RefPlDrivePort.EnableDrive(true);
	            (Fixture as RefPlatformSimpleTests).RefPlDrivePort.SetDrivePower(1.0, 1.0);
                yield break;
            }

			public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				//var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(simStateEntities.Single(NeedResetOnEachTry).State.Pose.Position));
				//@return(pos.Length() > 50);
				@return(false);
				yield break;
			}
        }
    }
}
