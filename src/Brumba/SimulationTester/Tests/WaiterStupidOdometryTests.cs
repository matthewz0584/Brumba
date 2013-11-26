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
		public ServiceForwarder ServiceForwarder { get; private set; }
		public Microsoft.Robotics.Services.Drive.Proxy.DriveOperations RefPlDrivePort { get; private set; }
		public WaiterStupid.Odometry.Proxy.OdometryOperations OdometryPort { get; set; }

		[SimSetUp]
		public void SetUp(ServiceForwarder serviceForwarder)
		{
			ServiceForwarder = serviceForwarder;
			RefPlDrivePort = serviceForwarder.ForwardTo<Microsoft.Robotics.Services.Drive.Proxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			OdometryPort = serviceForwarder.ForwardTo<WaiterStupid.Odometry.Proxy.OdometryOperations>("odometry@");
		}

		[SimTest]
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
                //Console.WriteLine("From Odometry ******{0}", odometryState.State.Pose);
                //Console.WriteLine("From Simulation ****{0}", SimPoseToEgocentricPose(simPose));
                //Console.WriteLine("Ratio **************{0}", poseDifference.Length() / SimPoseToEgocentricPose(simPose).Length());

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

				var thetaDifference = Math.Abs(odometryState.State.Pose.Z - SimPoseToEgocentricPose(simPose).Z);
                thetaDifference = Math.Min(thetaDifference, MathHelper.TwoPi - thetaDifference);
				Console.WriteLine("From Odometry ******{0}", odometryState.State.Pose.Z);
				Console.WriteLine("From Simulation ****{0}", SimPoseToEgocentricPose(simPose).Z);
                Console.WriteLine("Ratio **************{0}", thetaDifference / MathHelper.TwoPi);

                @return(!(_failed = thetaDifference / MathHelper.TwoPi > 0.05));
			}

			static Vector3 SimPoseToEgocentricPose(Pose pose)
			{
                return new Vector3(-pose.Position.Z, pose.Position.X, MathHelper.ToRadians(UIMath.QuaternionToEuler(pose.Orientation).Y));
			}
		}
	}
}
