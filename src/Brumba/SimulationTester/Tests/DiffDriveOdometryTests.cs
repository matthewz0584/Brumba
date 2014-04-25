using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.Simulation.EnvironmentBuilder;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Diagnostics;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using bPose = Brumba.WaiterStupid.Pose;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using xVector2 = Microsoft.Xna.Framework.Vector2;
using xVector3 = Microsoft.Xna.Framework.Vector3;

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
                var odometryPosition = new xVector2();
                yield return Fixture.OdometryPort.Get().Receive(os => odometryPosition = os.State.Pose.Position);
                
	            var simPosition = BoxWorldParser.SimToMap(ExtractStupidWaiterPose(simStateEntities).Position);

				@return(simPosition.EqualsRelatively(odometryPosition, 0.05));

                //Fixture.TesterService.LogInfo("From Odometry {0}", odometryPosition);
                //Fixture.TesterService.LogInfo("From Simulation {0}", simPosition);
                Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, (odometryPosition - simPosition).Length() / simPosition.Length());
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
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(-0.2, 0.2));
			}

			[Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				var angleFromOdometry = 0d;
				yield return Fixture.OdometryPort.Get().Receive(os => angleFromOdometry = os.State.Pose.Bearing);

				var angleFromSim = BoxWorldParser.SimToMap(ExtractStupidWaiterPose(simStateEntities).Orientation);

				var thetaDifference = MathHelper2.AngleDifference((float)angleFromOdometry, (float)angleFromSim);
				@return(thetaDifference / Math.Abs(angleFromOdometry) <= 0.05);

				//Fixture.TesterService.LogInfo("From Odometry {0}", angleFromOdometry);
				//Fixture.TesterService.LogInfo("From Odometry {0} truncated", MathHelper2.ToPositiveAngle(angleFromOdometry));
				//Fixture.TesterService.LogInfo("From Simulation {0}", MathHelper2.ToPositiveAngle(angleFromSim));
                Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, thetaDifference / Math.Abs(angleFromOdometry));
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
				var odometryPose = new bPose();
                yield return Fixture.OdometryPort.Get().Receive(os => odometryPose = os.State.Pose);

	            var simPosition = BoxWorldParser.SimToMap(ExtractStupidWaiterPose(simStateEntities).Position);
				var simBearing = BoxWorldParser.SimToMap(ExtractStupidWaiterPose(simStateEntities).Orientation);

				var thetaDifference = MathHelper2.AngleDifference((float)odometryPose.Bearing, (float)simBearing);
                @return(thetaDifference / Math.Abs(odometryPose.Bearing) <= 0.05 &&
                        //1.1 - radius of circle (1.1 - from sim, 1.06 - from calculation)
                        (odometryPose.Position - simPosition).Length() / (MathHelper.TwoPi * 1.1)  <= 0.05);

                Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, thetaDifference / Math.Abs(odometryPose.Bearing), (odometryPose.Position - simPosition).Length() / (MathHelper.TwoPi * 1.1));
            }
        }

		static rPose ExtractStupidWaiterPose(IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities)
		{
			return (rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose);
		}
	}

    [CategoryNamespace("http://brumba.ru/contracts/2012/11/simulationtester.html/waiterstupidodometrytests")]
    public enum LogCategory
    {
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Ratio1")]
        [CategoryArgument(1, "Ratio2")]
        ActualToExpectedRatio
    }
}
