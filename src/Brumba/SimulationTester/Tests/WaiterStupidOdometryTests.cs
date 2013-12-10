using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using VisualEntity = Microsoft.Robotics.Simulation.Engine.VisualEntity;

namespace Brumba.SimulationTester.Tests
{
	[SimTestFixture("waiter_stupid_odometry_tests", Wip = true)]
	public class WaiterStupidOdometryTests
	{
		public SimulationTesterService TesterService { get; private set; }
		public Microsoft.Robotics.Services.Drive.Proxy.DriveOperations RefPlDrivePort { get; private set; }
		public WaiterStupid.Odometry.Proxy.OdometryOperations OdometryPort { get; set; }

		[SimSetUp]
		public void SetUp(SimulationTesterService testerService)
		{
			TesterService = testerService;
			RefPlDrivePort = testerService.ForwardTo<Microsoft.Robotics.Services.Drive.Proxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			OdometryPort = testerService.ForwardTo<WaiterStupid.Odometry.Proxy.OdometryOperations>("odometry@");
		}

		//[SimTest]
		public class DriveStraight : StochasticTest
		{
            private bool _failed;

			public override void PrepareForReset(VisualEntity entity)
			{
			}

			public override IEnumerator<ITask> Start()
			{
			    _failed = false;
				EstimatedTime = 5;

				//Execs for synchronization, otherwise set power message can arrive before enable message
				yield return To.Exec((Fixture as WaiterStupidOdometryTests).RefPlDrivePort.EnableDrive(true));
				yield return To.Exec((Fixture as WaiterStupidOdometryTests).RefPlDrivePort.SetDrivePower(1.0, 1.0));
			}

			public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				if (elapsedTime < 0.9 * EstimatedTime || _failed)
				{
					@return(false);
					yield break;
				}

				var simPose = (Pose)DssTypeHelper.TransformFromProxy(simStateEntities.Single(epxy => epxy.State.Name == "stupid_waiter@").State.Pose);
				WaiterStupid.Odometry.Proxy.OdometryServiceState odometryState = null;
				yield return (Fixture as WaiterStupidOdometryTests).OdometryPort.Get().Receive(os => odometryState = os);

				var poseDifference = odometryState.State.Pose - SimPoseToEgocentricPose(simPose);

				(Fixture as WaiterStupidOdometryTests).TesterService.LogInfo("From Odometry {0}", odometryState.State.Pose);
				(Fixture as WaiterStupidOdometryTests).TesterService.LogInfo("From Simulation {0}", SimPoseToEgocentricPose(simPose));
				(Fixture as WaiterStupidOdometryTests).TesterService.LogInfo("Ratio {0}", poseDifference.Length() / SimPoseToEgocentricPose(simPose).Length());

                @return(!(_failed = poseDifference.Length() / SimPoseToEgocentricPose(simPose).Length() > 0.05));
			}

			static Vector3 SimPoseToEgocentricPose(Pose pose)
			{
				return new Vector3(-pose.Position.Z, pose.Position.X, UIMath.QuaternionToEuler(pose.Orientation).X);
			}
		}

		[SimTest]
		public class RotateInPlace : StochasticTest
		{
            private bool _failed;

			public override void PrepareForReset(VisualEntity entity)
			{
			}

			public override IEnumerator<ITask> Start()
			{
                _failed = false;
				EstimatedTime = 5;

				//Execs for synchronization, otherwise set power message can arrive before enable message
				yield return To.Exec((Fixture as WaiterStupidOdometryTests).RefPlDrivePort.EnableDrive(true));
				yield return To.Exec((Fixture as WaiterStupidOdometryTests).RefPlDrivePort.SetDrivePower(-0.2, 0.2));
			}

			public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
			{
                if (elapsedTime < 0.9 * EstimatedTime || _failed)
                {
                    @return(false);
                    yield break;
                }

				var simPose = (Pose)DssTypeHelper.TransformFromProxy(simStateEntities.Single(epxy => epxy.State.Name == "stupid_waiter@").State.Pose);
				WaiterStupid.Odometry.Proxy.OdometryServiceState odometryState = null;
				yield return (Fixture as WaiterStupidOdometryTests).OdometryPort.Get().Receive(os => odometryState = os);

				var thetaDifference = MathHelper2.AngleDifference(odometryState.State.Pose.Z, SimPoseToEgocentricPose(simPose).Z);//Math.Abs(ToPositiveAngle(odometryState.State.Pose.Z) - ToPositiveAngle(SimPoseToEgocentricPose(simPose).Z));
				@return(!(_failed = thetaDifference / MathHelper.TwoPi > 0.05));

                (Fixture as WaiterStupidOdometryTests).TesterService.LogInfo("From Odometry {0}", MathHelper2.ToPositiveAngle(odometryState.State.Pose.Z));
                (Fixture as WaiterStupidOdometryTests).TesterService.LogInfo("From Simulation {0}", MathHelper2.ToPositiveAngle(SimPoseToEgocentricPose(simPose).Z));
				(Fixture as WaiterStupidOdometryTests).TesterService.LogInfo("Ratio {0}", thetaDifference / MathHelper.TwoPi);
			}

			static Vector3 SimPoseToEgocentricPose(Pose pose)
			{
                return new Vector3(-pose.Position.Z, pose.Position.X, MathHelper.ToRadians(UIMath.QuaternionToEuler(pose.Orientation).Y));
			}
		}
	}
}
