using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Diagnostics;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using xVector2 = Microsoft.Xna.Framework.Vector2;
using xVector3 = Microsoft.Xna.Framework.Vector3;

namespace Brumba.SimulationTester.Tests
{
    [SimTestFixture("waiter_stupid_odometry_tests", Wip = true)]
	public class WaiterStupidOdometryTests
	{
		public SimulationTesterService TesterService { get; private set; }
		public Microsoft.Robotics.Services.Drive.Proxy.DriveOperations RefPlDrivePort { get; private set; }
		public WaiterStupid.Odometry.Proxy.OdometryOperations OdometryPort { get; set; }

		[SetUp]
		public void SetUp(SimulationTesterService testerService)
		{
			TesterService = testerService;
			RefPlDrivePort = testerService.ForwardTo<Microsoft.Robotics.Services.Drive.Proxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			OdometryPort = testerService.ForwardTo<WaiterStupid.Odometry.Proxy.OdometryOperations>("odometry@");
		}

		//[SimTest(8)]
		public class DriveStraight
		{
            [Fixture]
            public WaiterStupidOdometryTests Fixture { get; set; }

            [Start]
			public IEnumerator<ITask> Start()
			{
				//Execs for synchronization, otherwise set power message can arrive before enable message
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.5, 0.5));
			}

            [Test]
			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
			{
                var odometryPosition = new xVector2();
                yield return Fixture.OdometryPort.Get().Receive(os => odometryPosition = new xVector2(os.State.Pose.X, os.State.Pose.Y));
                
                var simPose = SimPoseToEgocentricPose(ExtractStupidWaiterPose(simStateEntities));
			    var simPosition = new xVector2(simPose.X, simPose.Y);

                @return((odometryPosition - simPosition).Length() / simPosition.Length() <= 0.05);

                Fixture.TesterService.LogInfo("From Odometry {0}", odometryPosition);
                Fixture.TesterService.LogInfo("From Simulation {0}", simPosition);
                Fixture.TesterService.LogInfo("Ratio {0}", (odometryPosition - simPosition).Length() / simPosition.Length());
			}
		}

		//[SimTest(4 * 2)]
		public class RotateOnPlace
		{
            [Fixture]
            public WaiterStupidOdometryTests Fixture { get; set; }

			[Start]
            public IEnumerator<ITask> Start()
			{
				//Execs for synchronization, otherwise set power message can arrive before enable message
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(-0.2, 0.2));

				//(Object as WaiterStupidOdometryTests).TesterService.LogInfo("*************");
			}

			[Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				var angleFromOdometry = 0f;
				yield return Fixture.OdometryPort.Get().Receive(os => angleFromOdometry = os.State.Pose.Z);

                //(Object as WaiterStupidOdometryTests).TesterService.LogInfo(Category.Time, elapsedTime);
                //(Object as WaiterStupidOdometryTests).TesterService.LogInfo(Category.Diff, MathHelper2.AngleDifference(angleFromOdometry, SimPoseToEgocentricPose(ExtractStupidWaiterPose(simStateEntities)).Z));

				var angleFromSim = SimPoseToEgocentricPose(ExtractStupidWaiterPose(simStateEntities)).Z;

				var thetaDifference = MathHelper2.AngleDifference(angleFromOdometry, angleFromSim);
				@return(thetaDifference / Math.Abs(angleFromOdometry) <= 0.05);

				Fixture.TesterService.LogInfo("From Odometry {0}", angleFromOdometry);
				Fixture.TesterService.LogInfo("From Odometry {0} truncated", MathHelper2.ToPositiveAngle(angleFromOdometry));
				Fixture.TesterService.LogInfo("From Simulation {0}", MathHelper2.ToPositiveAngle(angleFromSim));
				Fixture.TesterService.LogInfo("Ratio {0}", thetaDifference / Math.Abs(angleFromOdometry));
			}
		}

        [SimTest(6)]
        public class CircleTrajectory
        {
            [Fixture]
            public WaiterStupidOdometryTests Fixture { get; set; }

            [Start]
            public IEnumerator<ITask> Start()
            {
                //Execs for synchronization, otherwise set power message can arrive before enable message
                yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.3));

                //(Object as WaiterStupidOdometryTests).TesterService.LogInfo("*************");
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                var odometryPose = new xVector3();
                yield return Fixture.OdometryPort.Get().Receive(os => odometryPose = os.State.Pose);

                var simPose = SimPoseToEgocentricPose(ExtractStupidWaiterPose(simStateEntities));
                var simPosition = new xVector2(simPose.X, simPose.Y);

                var thetaDifference = MathHelper2.AngleDifference(odometryPose.Z, simPose.Z);
                @return(thetaDifference / Math.Abs(odometryPose.Z) <= 0.05 &&
                        (new xVector2(odometryPose.X, odometryPose.Y) - simPosition).Length() / simPosition.Length() <= 0.05);

                Fixture.TesterService.LogInfo("Ratio {0}", (new xVector2(odometryPose.X, odometryPose.Y) - simPosition).Length() / simPosition.Length());
            }
        }

		static xVector3 SimPoseToEgocentricPose(Pose pose)
		{
			return new xVector3(-pose.Position.Z, -pose.Position.X, MathHelper.ToRadians(UIMath.QuaternionToEuler(pose.Orientation).Y));
		}

		static Pose ExtractStupidWaiterPose(IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities)
		{
			return (Pose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose);
		}
	}

    [CategoryNamespace("http://brumba.ru/contracts/2012/11/simulationtester.html/waiterstupidodometrytests")]
    public enum Category
    {
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Value")]
        Time,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Value")]
        Diff
    }
}
