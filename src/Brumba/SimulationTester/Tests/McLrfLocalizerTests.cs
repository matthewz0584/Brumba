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

				var simPose = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose));

				@return(mcPose.Position.EqualsRelatively(simPose.Position, 0.1) &&
						MathHelper2.AngleDifference(mcPose.Bearing, simPose.Bearing).AlmostEqualWithError(0, 0.1));
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

				var simPose = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose));

				@return(mcPose.Position.EqualsRelatively(simPose.Position, 0.1) &&
						MathHelper2.AngleDifference(mcPose.Bearing, simPose.Bearing).AlmostEqualWithError(0, 0.1));
			}
		}

		[SimTest(14)]
		public class GlobalLocalizationCurvedPath : IStart, ITest
		{
			McLocalizationPxy.McLrfLocalizerOperations _mcLrfNotify = new McLocalizationPxy.McLrfLocalizerOperations();

			[Fixture]
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.McLrfLocalizationPort.InitPoseUnknown());
				yield return To.Exec(Fixture.McLrfLocalizationPort.Subscribe(_mcLrfNotify));
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));

				Fixture.TesterService.Activate(Arbiter.ReceiveWithIterator(false, _mcLrfNotify.P4, LogPoses));
				Fixture.TesterService.SpawnIterator(StraightRightStraightControls);
			}

			bPose _mcPose;
			bPose _simPose;
			IEnumerator<ITask> LogPoses(McLocalizationPxy.InitPose mcPoseMsg)
			{
				_mcPose = mcPoseMsg.Body.Pose;
				IEnumerable<VisualEntity> testeeEntitiesPxies = null;
				yield return To.Exec(Fixture.TesterService.GetTesteeEntities, (Action<IEnumerable<VisualEntity>>)(tep => testeeEntitiesPxies = tep));
				_simPose = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(testeeEntitiesPxies.Single().State.Pose));

				Fixture.TesterService.Activate(Arbiter.ReceiveWithIterator(false, _mcLrfNotify.P4, LogPoses));
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				//var mcPose = new bPose();
				//yield return Fixture.McLrfLocalizationPort.QueryPose().Receive(mcp => mcPose = mcp);

				//var simPose = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose));

				//@return(mcPose.Position.EqualsRelatively(simPose.Position, 0.1) &&
				//		MathHelper2.AngleDifference(mcPose.Bearing, simPose.Bearing).AlmostEqualWithError(0, 0.2));

				//Fixture.TesterService.LogInfo(LogCategory.ActualAndExpectedValues, mcPose, simPose);
				//Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, (mcPose.Position - simPose.Position).Length() / simPose.Position.Length());

				@return(_mcPose.Position.EqualsRelatively(_simPose.Position, 0.1) &&
						MathHelper2.AngleDifference(_mcPose.Bearing, _simPose.Bearing).AlmostEqualWithError(0, 0.2));

				Fixture.TesterService.LogInfo(LogCategory.ActualAndExpectedValues, _mcPose, _simPose);
				Fixture.TesterService.LogInfo(LogCategory.ActualToExpectedRatio, (_mcPose.Position - _simPose.Position).Length() / _simPose.Position.Length());
				yield break;
			}

			IEnumerator<ITask> StraightRightStraightControls()
			{
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.3, 0.3));

				var subscribeRq = Fixture.TesterService.Timer.Subscribe(6.5f);
				yield return To.Exec(subscribeRq.ResponsePort);
				yield return (subscribeRq.NotificationPort as BrTimerPxy.TimerOperations).P4.Receive();
				subscribeRq.NotificationShutdownPort.Post(new Shutdown());
				
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.3, 0.1));

				subscribeRq = Fixture.TesterService.Timer.Subscribe(1.65f);
				yield return To.Exec(subscribeRq.ResponsePort);
				yield return (subscribeRq.NotificationPort as BrTimerPxy.TimerOperations).P4.Receive();
				subscribeRq.NotificationShutdownPort.Post(new Shutdown());

				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.3, 0.3));
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

				var simPose = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose));

				@return(mcPose.Position.EqualsRelatively(simPose.Position, 0.1) &&
						MathHelper2.AngleDifference(mcPose.Bearing, simPose.Bearing).AlmostEqualWithError(0, 0.1));
			}
		}
	}
}