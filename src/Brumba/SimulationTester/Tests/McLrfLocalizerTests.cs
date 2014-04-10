using System;
using System.Collections.Generic;
using Brumba.DsspUtils;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Engine.Proxy;
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using McLocalizationPxy = Brumba.McLrfLocalizer.Proxy;

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

		[SimTest(20)]
		public class Tracking : IStart, ITest
		{
			[Fixture]
			public McLrfLocalizerTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
				yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(0.2, 0.2));
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				@return(false);
				yield break;
			}
		}
	}
}