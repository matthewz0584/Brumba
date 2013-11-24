using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using Quaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using drivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using lrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;
using engPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using odometryPxy = Brumba.WaiterStupid.Odometry.Proxy;
using VisualEntityPxy = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;

namespace Brumba.Simulation.SimulationTester.Tests
{
	[SimTestFixture("waiter_stupid_odometry_tests")]
	public class WaiterStupidOdometryTests
	{
		public ServiceForwarder ServiceForwarder { get; private set; }
		public drivePxy.DriveOperations RefPlDrivePort { get; private set; }
		public odometryPxy.OdometryOperations OdometryPort { get; set; }

		[SimSetUp]
		public void SetUp(ServiceForwarder serviceForwarder)
		{
			ServiceForwarder = serviceForwarder;
			RefPlDrivePort = serviceForwarder.ForwardTo<drivePxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			OdometryPort = serviceForwarder.ForwardTo<odometryPxy.OdometryOperations>("odometry@");
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

			public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<engPxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				if (elapsedTime < 0.9 * EstimatedTime || _failed)
				{
					@return(false);
					yield break;
				}

				var simPose = (Pose)DssTypeHelper.TransformFromProxy(simStateEntities.Single(epxy => epxy.State.Name == "stupid_waiter@").State.Pose);
				odometryPxy.OdometryServiceState odometryState = null;
				yield return (Fixture as WaiterStupidOdometryTests).OdometryPort.Get().Receive(os => odometryState = os);

				var poseDifference = odometryState.State.Pose - SimPoseToEgocentricPose(simPose);
                //Console.WriteLine("From Odometry ******{0}", odometryState.State.Pose);
                //Console.WriteLine("From Simulation ****{0}", SimPoseToEgocentricPose(simPose));
                //Console.WriteLine("Ratio **************{0}", poseDifference.Length() / odometryState.State.Pose.Length());

				@return(!(_failed = poseDifference.Length() / odometryState.State.Pose.Length() > 0.05));
			}

			static Vector3 SimPoseToEgocentricPose(Pose pose)
			{
				return new Vector3(-pose.Position.Z, pose.Position.X, UIMath.QuaternionToEuler(pose.Orientation).X);
			}
		}

		//[SimTest]
		public class RotateInPlace : StochasticTest
		{
			public override void PrepareForReset(VisualEntity entity)
			{
			}

			public override IEnumerator<ITask> Start()
			{
				EstimatedTime = 5;

				//Execs for synchronization, otherwise set power message can arrive before enable message
				yield return To.Exec((Fixture as WaiterStupidOdometryTests).RefPlDrivePort.EnableDrive(true));
				yield return To.Exec((Fixture as WaiterStupidOdometryTests).RefPlDrivePort.SetDrivePower(-1.0, 1.0));
			}

			public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<engPxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				if (elapsedTime < EstimatedTime - 1)
				{
					@return(false);
					yield break;
				}

				var simPose = (Pose)DssTypeHelper.TransformFromProxy(simStateEntities.Single(epxy => epxy.State.Name == "stupid_waiter@").State.Pose);
				odometryPxy.OdometryServiceState odometryState = null;
				yield return (Fixture as WaiterStupidOdometryTests).OdometryPort.Get().Receive(os => odometryState = os);

				var poseDifference = odometryState.State.Pose - SimPoseToEgocentricPose(simPose);
				Console.WriteLine("From Odometry ******{0}", odometryState.State.Pose);
				Console.WriteLine("From Simulation ****{0}", SimPoseToEgocentricPose(simPose));
				Console.WriteLine("Ratio **************{0}", poseDifference.Length() / odometryState.State.Pose.Length());
				@return(poseDifference.Length() / odometryState.State.Pose.Length() <= 0.01);
			}

			static Vector3 SimPoseToEgocentricPose(Pose pose)
			{
				return new Vector3(-pose.Position.Z, pose.Position.X, UIMath.QuaternionToEuler(pose.Orientation).X);
			}
		}
	}
}
