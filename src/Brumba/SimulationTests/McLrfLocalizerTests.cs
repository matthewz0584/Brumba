using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.Simulation;
using Brumba.Utils;
using Brumba.SimulationTester;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine.Proxy;
using RefPlPxy = Brumba.Simulation.SimulatedReferencePlatform2011.Proxy;
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using McLocalizationPxy = Brumba.McLrfLocalizer.Proxy;
using bPose = Brumba.Common.Pose;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using BrTimerPxy = Brumba.Entities.Timer.Proxy;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Brumba.SimulationTests
{
    [SimTestFixture("mc_lrf_localizer", PhysicsTimeStep = -1)]
	public class McLrfLocalizerTests
	{
		SimulationTesterService TesterService { get; set; }
		DrivePxy.DriveOperations RefPlDrivePort { get; set; }
		RefPlPxy.ReferencePlatform2011Operations RefPlatformSimulatedPort { get; set; }
		McLocalizationPxy.McLrfLocalizerOperations McLrfLocalizationPort { get; set; }
        McLocalizationPxy.McLrfLocalizerOperations McLrfLocalizationNotify { get; set; }

	    public bPose McPose { get; set; }
	    public bPose SimPose { get; set; }

	    [SetUp]
		public void SetUp(SimulationTesterService testerService)
		{
			TesterService = testerService;
			RefPlDrivePort = testerService.ForwardTo<DrivePxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			McLrfLocalizationPort = testerService.ForwardTo<McLocalizationPxy.McLrfLocalizerOperations>("localizer@");
			RefPlatformSimulatedPort = testerService.ForwardTo<RefPlPxy.ReferencePlatform2011Operations>("stupid_waiter_ref_platform");
            McLrfLocalizationNotify = new McLocalizationPxy.McLrfLocalizerOperations();
            McLrfLocalizationPort.Subscribe(McLrfLocalizationNotify);

            TesterService.Activate(Arbiter.ReceiveWithIterator<McLocalizationPxy.InitPose>(false, McLrfLocalizationNotify, GetPoses));
		}

	    IEnumerator<ITask> GetPoses(McLocalizationPxy.InitPose mcPoseMsg)
        {
            McPose = (bPose)DssTypeHelper.TransformFromProxy(mcPoseMsg.Body.Pose);

            IEnumerable<VisualEntity> testeeEntitiesPxies = null;
            yield return To.Exec(TesterService.GetTesteeEntityProxies, (Action<IEnumerable<VisualEntity>>)(tep => testeeEntitiesPxies = tep));
            SimPose = ((rPose)DssTypeHelper.TransformFromProxy(testeeEntitiesPxies.Single().State.Pose)).SimToMap();

            TesterService.Activate(Arbiter.ReceiveWithIterator<McLocalizationPxy.InitPose>(false, McLrfLocalizationNotify, GetPoses));
        }

	    void LogResults()
	    {
            TesterService.LogInfo(LogCategory.ActualAndExpectedValues, McPose, SimPose);
			TesterService.LogInfo(LogCategory.ActualToExpectedRatio, (McPose.Position - SimPose.Position).Length(), MathHelper2.AngleDifference(McPose.Bearing, SimPose.Bearing));
	    }

        private IEnumerator<ITask> Wait(float time)
        {
            var subscribeRq = TesterService.Timer.Subscribe(time);
            yield return To.Exec(subscribeRq.ResponsePort);
            yield return (subscribeRq.NotificationPort as BrTimerPxy.TimerOperations).P4.Receive();
            subscribeRq.NotificationShutdownPort.Post(new Shutdown());
        }

		[SimTest(7.1f)]
		public class TrackingStraightPath : IStart, ITest, IFixture<McLrfLocalizerTests>
		{
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPose(new Common.Proxy.Pose(new Vector2(1.7f, 3.25f), 0)));
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
				
                //Localization update takes time (~0.3s), if robot keeps moving mclrf position estimate lags for this time (multiplied by velocity)
                //Thus in order to get accurate estimate I should stop the robot before assessing test results.
                Fixture.TesterService.SpawnIterator(StraightAndStop);
			}

		    private IEnumerator<ITask> StraightAndStop()
		    {
                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.5, 0.5));

		        yield return To.Exec(Fixture.Wait, 5f);

		        yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0, 0));
		    }

		    public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simEntities, double elapsedTime)
			{
                @return((Fixture.McPose.Position - Fixture.SimPose.Position).Length().AlmostEqualWithError(0, 0.2) &&
                        MathHelper2.AngleDifference(Fixture.McPose.Bearing, Fixture.SimPose.Bearing).AlmostEqualWithError(0, 0.05));
                Fixture.LogResults();
                yield break;
			}
		}

        [SimTest(11.1f)]
        public class TrackingCurvedPath : IStart, ITest, IFixture<McLrfLocalizerTests>
        {
            public McLrfLocalizerTests Fixture { get; set; }

            public IEnumerator<ITask> Start()
            {
                yield return To.Exec(Fixture.McLrfLocalizationPort.InitPose(new Common.Proxy.Pose(new Vector2(1.7f, 3.25f), 0)));
                yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));

                Fixture.TesterService.SpawnIterator(StraightRightStraightControls);
            }

            IEnumerator<ITask> StraightRightStraightControls()
            {
                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.4));

                yield return To.Exec(Fixture.Wait, 4.7f);

                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.1));

                yield return To.Exec(Fixture.Wait, 1.15f);

                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.4));

                yield return To.Exec(Fixture.Wait, 2f);

                //Driving out of map, better not to stop
                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0, 0));
            }

            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simEntities, double elapsedTime)
            {
                @return((Fixture.McPose.Position - Fixture.SimPose.Position).Length().AlmostEqualWithError(0, 0.6) &&
                        MathHelper2.AngleDifference(Fixture.McPose.Bearing, Fixture.SimPose.Bearing).AlmostEqualWithError(0, 0.4));
                Fixture.LogResults();
                yield break;
            }
        }

		[SimTest(12.1f)]
        public class GlobalLocalizationStraightPath : IStart, ITest, IFixture<McLrfLocalizerTests>
		{
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPoseUnknown());
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));

                Fixture.TesterService.SpawnIterator(StraightAndStop);
			}

            private IEnumerator<ITask> StraightAndStop()
            {
                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.4));

                yield return To.Exec(Fixture.Wait, 11f);

                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0, 0));
            }

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simEntities, double elapsedTime)
			{
                @return((Fixture.McPose.Position - Fixture.SimPose.Position).Length().AlmostEqualWithError(0, 0.3) &&
                        MathHelper2.AngleDifference(Fixture.McPose.Bearing, Fixture.SimPose.Bearing).AlmostEqualWithError(0, 0.3));
                Fixture.LogResults();
                yield break;
            }
		}

		[SimTest(11.1f)]
        public class GlobalLocalizationCurvedPath : IStart, ITest, IFixture<McLrfLocalizerTests>
		{
			public McLrfLocalizerTests Fixture { get; set; }

            public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPoseUnknown());
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));

				Fixture.TesterService.SpawnIterator(StraightRightStraightControls);
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simEntities, double elapsedTime)
			{
				@return((Fixture.McPose.Position - Fixture.SimPose.Position).Length().AlmostEqualWithError(0, 0.6) &&
                        MathHelper2.AngleDifference(Fixture.McPose.Bearing, Fixture.SimPose.Bearing).AlmostEqualWithError(0, 0.4));
                Fixture.LogResults();
				yield break;
			}

			IEnumerator<ITask> StraightRightStraightControls()
			{
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.4));

                yield return To.Exec(Fixture.Wait, 4.7f);
				
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.1));

                yield return To.Exec(Fixture.Wait, 1.15f);

				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.4));

                yield return To.Exec(Fixture.Wait, 4f);

                //Driving out of map, better not to stop
                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0, 0));
			}
		}

		//[SimTest(7)]
        public class TrackingWithFailingOdometry : IStart, ITest, IFixture<McLrfLocalizerTests>
		{
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPose(new Common.Proxy.Pose(new Vector2(1.7f, 3.25f), 0)));
				yield return To.Exec(Fixture.RefPlatformSimulatedPort.UpdateWheelTicksSigma(new Vector2(5)));
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.6, 0.6));
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simEntities, double elapsedTime)
			{
                @return(Fixture.McPose.Position.EqualsRelatively(Fixture.SimPose.Position, 0.1) &&
                        MathHelper2.AngleDifference(Fixture.McPose.Bearing, Fixture.SimPose.Bearing).AlmostEqualWithError(0, 0.1));
                Fixture.LogResults();
                yield break;
            }
		}
	}
}