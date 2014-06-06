using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.Simulation.EnvironmentBuilder;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine.Proxy;
using RefPlPxy = Brumba.Simulation.SimulatedReferencePlatform2011.Proxy;
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using McLocalizationPxy = Brumba.McLrfLocalizer.Proxy;
using bPose = Brumba.WaiterStupid.Pose;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using BrTimerPxy = Brumba.Entities.Timer.Proxy;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Brumba.SimulationTester.Tests
{
    [SimTestFixture("mc_lrf_localizer", Wip = true, PhysicsTimeStep = -1)]
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

            TesterService.Activate(Arbiter.ReceiveWithIterator(false, McLrfLocalizationNotify.P4, GetPoses));
		}

	    IEnumerator<ITask> GetPoses(McLocalizationPxy.InitPose mcPoseMsg)
        {
            McPose = mcPoseMsg.Body.Pose;

            IEnumerable<VisualEntity> testeeEntitiesPxies = null;
            yield return To.Exec(TesterService.GetTesteeEntityProxies, (Action<IEnumerable<VisualEntity>>)(tep => testeeEntitiesPxies = tep));
            SimPose = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(testeeEntitiesPxies.Single().State.Pose));

            TesterService.Activate(Arbiter.ReceiveWithIterator(false, McLrfLocalizationNotify.P4, GetPoses));
        }

	    void LogResults()
	    {
            TesterService.LogInfo(LogCategory.ActualAndExpectedValues, McPose, SimPose);
            TesterService.LogInfo(LogCategory.ActualToExpectedRatio, (McPose.Position - SimPose.Position).Length() / SimPose.Position.Length());
	    }

		[SimTest(5)]
		public class Tracking : IStart, ITest
		{
			[Fixture]
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPose(new bPose(new Vector2(1.7f, 3.25f), 0)));
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.6, 0.6));
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
                @return(Fixture.McPose.Position.EqualsRelatively(Fixture.SimPose.Position, 0.1) &&
                        MathHelper2.AngleDifference(Fixture.McPose.Bearing, Fixture.SimPose.Bearing).AlmostEqualWithError(0, 0.1));
                Fixture.LogResults();
                yield break;
			}
		}

		[SimTest(9)]
		public class GlobalLocalizationStraightPath : IStart, ITest
		{
			[Fixture]
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPoseUnknown());
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.4));
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
                @return(Fixture.McPose.Position.EqualsRelatively(Fixture.SimPose.Position, 0.1) &&
                        MathHelper2.AngleDifference(Fixture.McPose.Bearing, Fixture.SimPose.Bearing).AlmostEqualWithError(0, 0.2));
                Fixture.LogResults();
                yield break;
            }
		}

		[SimTest(10)]
		public class GlobalLocalizationCurvedPath : IStart, ITest
		{
			[Fixture]
			public McLrfLocalizerTests Fixture { get; set; }

            public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPoseUnknown());
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));

				Fixture.TesterService.SpawnIterator(StraightRightStraightControls);
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
                @return(Fixture.McPose.Position.EqualsRelatively(Fixture.SimPose.Position, 0.1) &&
                        MathHelper2.AngleDifference(Fixture.McPose.Bearing, Fixture.SimPose.Bearing).AlmostEqualWithError(0, 0.2));
                Fixture.LogResults();
				yield break;
			}

			IEnumerator<ITask> StraightRightStraightControls()
			{
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.4));

				var subscribeRq = Fixture.TesterService.Timer.Subscribe(5f);
				yield return To.Exec(subscribeRq.ResponsePort);
				yield return (subscribeRq.NotificationPort as BrTimerPxy.TimerOperations).P4.Receive();
				subscribeRq.NotificationShutdownPort.Post(new Shutdown());
				
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.1));

				subscribeRq = Fixture.TesterService.Timer.Subscribe(1.15f);
				yield return To.Exec(subscribeRq.ResponsePort);
				yield return (subscribeRq.NotificationPort as BrTimerPxy.TimerOperations).P4.Receive();
				subscribeRq.NotificationShutdownPort.Post(new Shutdown());

				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.4));
			}
		}

		//[SimTest(7)]
		public class TrackingWithFailingOdometry : IStart, ITest
		{
			[Fixture]
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPose(new bPose(new Vector2(1.7f, 3.25f), 0)));
				yield return To.Exec(Fixture.RefPlatformSimulatedPort.UpdateWheelTicksSigma(new Vector2(5)));
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.6, 0.6));
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
                @return(Fixture.McPose.Position.EqualsRelatively(Fixture.SimPose.Position, 0.1) &&
                        MathHelper2.AngleDifference(Fixture.McPose.Bearing, Fixture.SimPose.Bearing).AlmostEqualWithError(0, 0.1));
                Fixture.LogResults();
                yield break;
            }
		}
	}
}