using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.Simulation;
using Brumba.SimulationTester;
using Brumba.Simulation.EnvironmentBuilder;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Diagnostics;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using bPose = Brumba.Common.Pose;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using xVector2 = Microsoft.Xna.Framework.Vector2;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using xQuaternion = Microsoft.Xna.Framework.Quaternion;
using rVector2 = Microsoft.Robotics.PhysicalModel.Vector2;
using rVector3 = Microsoft.Robotics.PhysicalModel.Vector3;
using rQuaternion = Microsoft.Robotics.PhysicalModel.Quaternion;

namespace Brumba.SimulationTests
{
    [SimTestFixture("diff_drive_odometry")]
	public class DiffDriveOdometryTests
	{
		public SimulationTesterService TesterService { get; private set; }
		public Microsoft.Robotics.Services.Drive.Proxy.DriveOperations RefPlDrivePort { get; private set; }
		public DiffDriveOdometry.Proxy.DiffDriveOdometryOperations OdometryPort { get; set; }

        public void Populate()
        {
            EnvironmentBuilderService.PopulateSimpleEnvironment();

            SimulationEngine.GlobalInstancePort.Insert(EnvironmentBuilderService.BuildWaiter1("stupid_waiter", "stupid_waiter_lidar", new rPose(new rVector3(), rQuaternion.FromAxisAngle(0, 1, 0, MathHelper.Pi))));
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1.0f, new rPose(), new rVector3(1, 1, 1))), new rVector3(8, 0.501f, 0)) { State = { Name = "golden_brick_out_of_range" } });
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1.0f, new rPose(), new rVector3(1, 1, 1))), new rVector3(-5f, 0.501f, 0)) { State = { Name = "golden_brick_in_range" } });
        }

		[SetUp]
		public void SetUp(SimulationTesterService testerService)
		{
			TesterService = testerService;
			RefPlDrivePort = testerService.ForwardTo<Microsoft.Robotics.Services.Drive.Proxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
            OdometryPort = testerService.ForwardTo<DiffDriveOdometry.Proxy.DiffDriveOdometryOperations>("odometry@");
		}

        [SimTest(8, IsProbabilistic = false)]
		public class DriveStraight
		{
            [Fixture]
            public DiffDriveOdometryTests Fixture { get; set; }

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
                DiffDriveOdometry.Proxy.DiffDriveOdometryServiceState odometry = null;
                yield return Fixture.OdometryPort.Get().Receive(os => odometry = os);

                var simPosition = ExtractPose(simStateEntities).Position.SimToMap();
                var simBearing = ExtractPose(simStateEntities).Orientation.SimToMap();
                var simVelocity = ExtractVelocity(simStateEntities).SimToMap();

                //Velocity component total differential (left and right wheels ticks are variables) is WheelRadius * RadiansPerTick / deltaT * (deltaTicksL + deltaTicksR)
                //Which equals 0.13 given test constants and possible offset by 1 tick due to ticks and time delta discretization
                //Similar calculation with angular velocity gives 0.44 error for 1 tick
                @return(simPosition.EqualsRelatively(odometry.Pose.Position, 0.1) && odometry.Pose.Bearing.ToPositiveAngle().AlmostEqualWithError(simBearing, 0.05) &&
                        odometry.Velocity.Linear.AlmostEqualWithAbsoluteError(simVelocity.Length(), simVelocity.Length() - odometry.Velocity.Linear, 0.13) && odometry.Velocity.Angular.AlmostEqualWithError(0, 0.44));

                //Fixture.TesterService.LogInfo("From Odometry {0}", odometryPosition);
                //Fixture.TesterService.LogInfo("From Simulation {0}", simPosition);
                Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, (odometry.Pose.Position - simPosition).Length() / simPosition.Length());
			}
		}

        [SimTest(4 * 2, IsProbabilistic = false)]
        public class RotateOnPlace
        {
            [Fixture]
            public DiffDriveOdometryTests Fixture { get; set; }

            [Start]
            public IEnumerator<ITask> Start()
            {
                //Execs for synchronization, otherwise set power message can arrive before enable message
                yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
                //yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(-0.2, 0.2));
                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(-0.6, 0.6));
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                DiffDriveOdometry.Proxy.DiffDriveOdometryServiceState odometry = null;
                yield return Fixture.OdometryPort.Get().Receive(os => odometry = os);

                var simBearing = ExtractPose(simStateEntities).Orientation.SimToMap();

                var thetaDifference = MathHelper2.AngleDifference(odometry.Pose.Bearing, simBearing);
                @return(thetaDifference / Math.Abs(odometry.Pose.Bearing) <= 0.02 && odometry.Pose.Position.EqualsWithError(new Vector2(), 0.1) &&
                //Velocity component total differential (left and right wheels ticks are variables) is 2 * WheelRadius * RadiansPerTick / (deltaT * WheelBase) * (deltaTicksR - deltaTicksL),
                //Which equals 0.44 * 2 given test constants and possible offset by 1 tick due to ticks and time delta discretization
                //Velocity from simulation is too noisy (but generally corresponds with predicted result), so I compare with velocity calculated using simulation entity code constants,
                //Which occures to be equal to 10.05 * p for this test settings
                        odometry.Velocity.Angular.AlmostEqualWithError(5.19, 0.45) && odometry.Velocity.Linear.AlmostEqualWithError(0, 0.2));
                
                Fixture.TesterService.LogInfo(LogCategory.ActualAndExpectedValues, odometry.Velocity.Angular, 6, "Angular velocity");
                //Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, thetaDifference / Math.Abs(odometry.Pose.Bearing));
            }
        }

        [SimTest(15, IsProbabilistic = false)]
        public class CircleTrajectory
        {
            [Fixture]
            public DiffDriveOdometryTests Fixture { get; set; }

            [Start]
            public IEnumerator<ITask> Start()
            {
                //Execs for synchronization, otherwise set power message can arrive before enable message
                yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.3));
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                DiffDriveOdometry.Proxy.DiffDriveOdometryServiceState odometry = null;
                yield return Fixture.OdometryPort.Get().Receive(os => odometry = os);

                var simPosition = ExtractPose(simStateEntities).Position.SimToMap();
                var simVelocity = ExtractVelocity(simStateEntities).SimToMap();
                var simBearing = ExtractPose(simStateEntities).Orientation.SimToMap();
                var simAngularVelocity = ExtractAngularVelocity(simStateEntities).SimToMapAngularVelocity();

                //1.4 - experimental radius of circle trajectory
                @return(simPosition.EqualsRelatively(odometry.Pose.Position, 0.1) && odometry.Pose.Bearing.ToPositiveAngle().AlmostEqualWithError(simBearing, 0.05) &&
                //Angular velocity hits almost always to correct value, i.e. no errors due to discretization occures
                //Linear velocity could have error in both coordinates, so absolute error is 0.18 = (0.13^2 + 0.13^2)^0.5
                        odometry.Velocity.Linear.AlmostEqualWithError(simVelocity.Length(), 0.18) && odometry.Velocity.Angular.AlmostEqualWithAbsoluteError(simAngularVelocity, odometry.Velocity.Angular - simAngularVelocity, 0.1));

                //Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, thetaDifference / Math.Abs(odometry.Pose.Bearing), (odometry.Pose.Position - simPosition).Length() / (MathHelper.TwoPi * 1.1), "position");
                //Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, (simVelocity - odometry.Velocity.Position).Length(), 0, "linear velocity");
                //Fixture.TesterService.LogInfo(LogCategory.ActualAndExpectedValues, odometry.Velocity.Position, simVelocity, "linear velocity");
                //Fixture.TesterService.LogInfo(LogCategory.ActualAndExpectedValues, odometry.Velocity.Bearing, simAngularVelocity, "angular velocity");
            }
        }

		public static rPose ExtractPose(IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities)
		{
			return (rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose);
		}

        static rVector3 ExtractVelocity(IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities)
        {
            return (rVector3)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Velocity);
        }

        static rVector3 ExtractAngularVelocity(IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities)
        {
            return (rVector3)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.AngularVelocity);
        }
	}

    [CategoryNamespace("http://brumba.ru/contracts/2012/11/simulationtester.html/odometrytests")]
    public enum LogCategory
    {
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Ratio1")]
        [CategoryArgument(1, "Ratio2")]
        [CategoryArgument(2, "Description")]
        ActualToExpectedRatio,
		[OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
		[CategoryArgument(0, "Actual value")]
		[CategoryArgument(1, "Expected value")]
        [CategoryArgument(2, "Description")]
		ActualAndExpectedValues
    }
}
