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
using Microsoft.Xna.Framework;
using RefPlPxy = Brumba.Simulation.SimulatedReferencePlatform2011.Proxy;
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using McLocalizationPxy = Brumba.McLrfLocalizer.Proxy;
using bPose = Brumba.WaiterStupid.Pose;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using BrTimerPxy = Brumba.Entities.Timer.Proxy;

namespace Brumba.SimulationTester.Tests
{
	[SimTestFixture("mc_lrf_localizer", Wip = true)]
	public class McLrfLocalizerTests
	{
		public SimulationTesterService TesterService { get; private set; }
		public DrivePxy.DriveOperations RefPlDrivePort { get; private set; }
		public RefPlPxy.ReferencePlatform2011Operations RefPlatformSimulatedPort { get; private set; }
		public McLocalizationPxy.McLrfLocalizerOperations McLrfLocalizationPort { get; private set; }

		[SetUp]
		public void SetUp(SimulationTesterService testerService)
		{
			TesterService = testerService;
			RefPlDrivePort = testerService.ForwardTo<DrivePxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			McLrfLocalizationPort = testerService.ForwardTo<McLocalizationPxy.McLrfLocalizerOperations>("localizer@");
			RefPlatformSimulatedPort = testerService.ForwardTo<RefPlPxy.ReferencePlatform2011Operations>("stupid_waiter_ref_platform");
		}

		//[SimTest(5)]
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
				var mcPose = new bPose();
				yield return Fixture.McLrfLocalizationPort.QueryPose().Receive(mcp => mcPose = mcp);

				var simPosition = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose));

				@return(mcPose.Position.EqualsRelatively(simPosition.Position, 0.1) &&
						MathHelper2.AngleDifference(mcPose.Bearing, simPosition.Bearing).AlmostEqualWithError(0, 0.1));
			}
		}

		//[SimTest(10)]
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
				var mcPose = new bPose();
				yield return Fixture.McLrfLocalizationPort.QueryPose().Receive(mcp => mcPose = mcp);

				var simPosition = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose));

				@return(mcPose.Position.EqualsRelatively(simPosition.Position, 0.1) &&
						MathHelper2.AngleDifference(mcPose.Bearing, simPosition.Bearing).AlmostEqualWithError(0, 0.1));
			}
		}

		[SimTest(10)]
		public class GlobalLocalizationCurvedPath : IStart, ITest
		{
			[Fixture]
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				Fixture.TesterService.SpawnIterator(StraightRightStraightControls);
				
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPoseUnknown());
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				var mcPose = new bPose();
				yield return Fixture.McLrfLocalizationPort.QueryPose().Receive(mcp => mcPose = mcp);

				var simPosition = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose));

				@return(mcPose.Position.EqualsRelatively(simPosition.Position, 0.1) &&
						MathHelper2.AngleDifference(mcPose.Bearing, simPosition.Bearing).AlmostEqualWithError(0, 0.1));
			}

			IEnumerator<ITask> StraightRightStraightControls()
			{
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.4));

				var subscribeRq = Fixture.TesterService.Timer.Subscribe(4.5f);
				yield return To.Exec(subscribeRq.ResponsePort);
				yield return (subscribeRq.NotificationPort as BrTimerPxy.TimerOperations).P4.Receive();
				subscribeRq.NotificationShutdownPort.Post(new Shutdown());
				
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.4, 0.1));

				subscribeRq = Fixture.TesterService.Timer.Subscribe(1);
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
				var mcPose = new bPose();
				yield return Fixture.McLrfLocalizationPort.QueryPose().Receive(mcp => mcPose = mcp);

				var simPosition = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose));

				@return(mcPose.Position.EqualsRelatively(simPosition.Position, 0.1) &&
						MathHelper2.AngleDifference(mcPose.Bearing, simPosition.Bearing).AlmostEqualWithError(0, 0.1));
			}
		}
	}
}