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
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using McLocalizationPxy = Brumba.McLrfLocalizer.Proxy;
using bPose = Brumba.WaiterStupid.Pose;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;

namespace Brumba.SimulationTester.Tests
{
	[SimTestFixture("mc_lrf_localizer", Wip = true)]
	public class McLrfLocalizerTests
	{
		public SimulationTesterService TesterService { get; private set; }
		public DrivePxy.DriveOperations RefPlDrivePort { get; private set; }
		public McLocalizationPxy.McLrfLocalizerOperations McLrfLocalizationPort { get; private set; }

		[SetUp]
		public void SetUp(SimulationTesterService testerService)
		{
			TesterService = testerService;
			RefPlDrivePort = testerService.ForwardTo<DrivePxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			McLrfLocalizationPort = testerService.ForwardTo<McLocalizationPxy.McLrfLocalizerOperations>("localizer@");
		}

		[SimTest(5)]
		public class Tracking : IStart, ITest
		{
			[Fixture]
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.6, 0.6));
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				var mcPose = new bPose();
				yield return Fixture.McLrfLocalizationPort.Get().Receive(mcs => mcPose = (bPose) DssTypeHelper.TransformFromProxy(mcs.FirstPoseCandidate));

				var simPosition = BoxWorldParser.SimToMap((rPose)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose));

				@return(mcPose.Position.EqualsRelatively(simPosition.Position, 0.1) &&
						MathHelper2.AngleDifference(mcPose.Bearing, simPosition.Bearing).AlmostEqualWithError(0, 0.1));
			}
		}
	}
}