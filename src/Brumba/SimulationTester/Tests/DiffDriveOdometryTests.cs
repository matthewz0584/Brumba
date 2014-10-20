using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.Simulation.EnvironmentBuilder;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Diagnostics;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using bPose = Brumba.WaiterStupid.Pose;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using xVector2 = Microsoft.Xna.Framework.Vector2;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using rVector2 = Microsoft.Robotics.PhysicalModel.Vector2;
using rVector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.SimulationTester.Tests
{
	[SimTestFixture("diff_drive_odometry", Wip = true)]
	public class DiffDriveOdometryTests
	{
		public SimulationTesterService TesterService { get; private set; }
		public Microsoft.Robotics.Services.Drive.Proxy.DriveOperations RefPlDrivePort { get; private set; }
		public DiffDriveOdometry.Proxy.DiffDriveOdometryOperations OdometryPort { get; set; }

		[SetUp]
		public void SetUp(SimulationTesterService testerService)
		{
			TesterService = testerService;
			RefPlDrivePort = testerService.ForwardTo<Microsoft.Robotics.Services.Drive.Proxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
            OdometryPort = testerService.ForwardTo<DiffDriveOdometry.Proxy.DiffDriveOdometryOperations>("odometry@");
		}

		[SimTest(8)]
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
                DiffDriveOdometry.Proxy.DiffDriveOdometryState odometry = null;
                yield return Fixture.OdometryPort.Get().Receive(os => odometry = os.State);
                
	            var simPosition = BoxWorldParser.SimToMap(ExtractPose(simStateEntities).Position);
                var simVelocity = BoxWorldParser.SimToMap(ExtractVelocity(simStateEntities));

                //Velocity component total differential (left and right wheels ticks are variables) is WheelRadius * RadiansPerTick / deltaT * cos(Bearing) * (deltaTicksL + deltaTicksR)
                //Which equals 0.13 given test constants and possible offset by 1 tick due to ticks and time delta discretization
                @return(simPosition.EqualsRelatively(odometry.Pose.Position, 0.05) && odometry.Pose.Bearing.AlmostEqualWithError(0, 0.01) &&
                        simVelocity.EqualsRelatively(odometry.Velocity.Position, 0.13 / simVelocity.Length()) && odometry.Velocity.Bearing.AlmostEqualWithError(0, 0.01));

                //Fixture.TesterService.LogInfo("From Odometry {0}", odometryPosition);
                //Fixture.TesterService.LogInfo("From Simulation {0}", simPosition);
                Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, (odometry.Pose.Position - simPosition).Length() / simPosition.Length());
			}
		}

        [SimTest(4 * 2)]
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
                DiffDriveOdometry.Proxy.DiffDriveOdometryState odometry = null;
                yield return Fixture.OdometryPort.Get().Receive(os => odometry = os.State);

                var simBearing = BoxWorldParser.SimToMap(ExtractPose(simStateEntities).Orientation);

                var thetaDifference = MathHelper2.AngleDifference(odometry.Pose.Bearing, simBearing);
                @return(thetaDifference / Math.Abs(odometry.Pose.Bearing) <= 0.02 && odometry.Pose.Position == new Vector2() &&
                //Velocity component total differential (left and right wheels ticks are variables) is 2 * WheelRadius * RadiansPerTick / (deltaT * WheelBase) * (deltaTicksR - deltaTicksL),
                //Which equals 0.44 * 2 given test constants and possible offset by 1 tick due to ticks and time delta discretization
                //Velocity from simulation is too noisy (but generally corresponds with predicted result), so I compare with velocity calculated using simulation entity code constants,
                //Which occures to be equal to 10.05 * p for this test settings
                        odometry.Velocity.Bearing.AlmostEqualWithError(6, 0.45) && odometry.Velocity.Position == new Vector2());
                
                Fixture.TesterService.LogInfo(LogCategory.ActualAndExpectedValues, odometry.Velocity.Bearing, 6, "Angular velocity");
                //Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, thetaDifference / Math.Abs(odometry.Pose.Bearing));
            }
        }

        [SimTest(12 + 1)]//Full circle time + correction for acceleration
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
                DiffDriveOdometry.Proxy.DiffDriveOdometryState odometry = null;
                yield return Fixture.OdometryPort.Get().Receive(os => odometry = os.State);

	            var simPosition = BoxWorldParser.SimToMap(ExtractPose(simStateEntities).Position);
                var simVelocity = BoxWorldParser.SimToMap(ExtractVelocity(simStateEntities));
				var simBearing = BoxWorldParser.SimToMap(ExtractPose(simStateEntities).Orientation);
                var simAngularVelocity = BoxWorldParser.SimToMapAngularVelocity(ExtractAngularVelocity(simStateEntities));

				var thetaDifference = MathHelper2.AngleDifference((float)odometry.Pose.Bearing, (float)simBearing);
                //1.1 - radius of circle trajectory (1.1 - from sim, 1.06 - theoretical)
                @return(thetaDifference / Math.Abs(odometry.Pose.Bearing) <= 0.05 && (odometry.Pose.Position - simPosition).Length() / (MathHelper.TwoPi * 1.1) <= 0.05 &&
                //Angular velocity hits almost always to correct value, i.e. no errors due to discretization occures
                //Linear velocity could have error in both coordinates, so absolute error is 0.18 = (0.13^2 + 0.13^2)^0.5
                        odometry.Velocity.Position.EqualsWithError(simVelocity, 0.18) && odometry.Velocity.Bearing.AlmostEqualWithAbsoluteError(simAngularVelocity, odometry.Velocity.Bearing - simAngularVelocity, 0.1));

                //Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, thetaDifference / Math.Abs(odometry.Pose.Bearing), (odometry.Pose.Position - simPosition).Length() / (MathHelper.TwoPi * 1.1), "position");
                //Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, (simVelocity - odometry.Velocity.Position).Length(), 0, "linear velocity");
                //Fixture.TesterService.LogInfo(LogCategory.ActualAndExpectedValues, odometry.Velocity.Position, simVelocity, "linear velocity");
                //Fixture.TesterService.LogInfo(LogCategory.ActualAndExpectedValues, odometry.Velocity.Bearing, simAngularVelocity, "angular velocity");
            }
        }

		static rPose ExtractPose(IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities)
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

    [CategoryNamespace("http://brumba.ru/contracts/2012/11/simulationtester.html/waiterstupidodometrytests")]
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
