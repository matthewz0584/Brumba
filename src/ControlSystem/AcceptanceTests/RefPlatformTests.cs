﻿using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Common;
using Brumba.SimulationTestRunner;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using SickLrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;
using VisualEntity = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;

namespace Brumba.AcceptanceTests
{
    [SimTestFixture("ref_platform")]
    public class RefPlatformTests
    {
		public SimulationTestRunnerService TesterService { get; private set; }
        public DrivePxy.DriveOperations RefPlDrivePort { get; private set; }
        public SickLrfPxy.SickLRFOperations SickLrfPort { get; private set; }

        [SetUp]
        public void SetUp(SimulationTestRunnerService testerService)
        {
            TesterService = testerService;
            RefPlDrivePort = testerService.ForwardTo<DrivePxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
            SickLrfPort = testerService.ForwardTo<SickLrfPxy.SickLRFOperations>("stupid_waiter_lidar/sicklrf");
        }

        //Max speed = 1,5 m/s, distance 2 meters, plus correction for accelerating from 0 to set speed 
        [SimTest(1.7f, IsProbabilistic = false)]
        public class DriveForwardTest
        {
            [Fixture]
            public RefPlatformTests Fixture { get; set; }

	        [Start]
            public IEnumerator<ITask> Start()
            {
                //Workaround for the case, when Entity service has not yet connected to reinserted entity
                yield return Fixture.TesterService.Timeout(500);
                //Execs for synchronization, otherwise set power message can arrive before enable message
                yield return To.Exec(Fixture.RefPlDrivePort.EnableDrive(true));
                yield return To.Exec(Fixture.RefPlDrivePort.SetDrivePower(1.0, 1.0));
            }

			[Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose.Position));
				@return(pos.Length() > 2);
				yield break;
			}
        }

        [SimTest(1, IsProbabilistic = false)]
		public class LrfTest
		{
            Port<SickLrfPxy.Replace> _lrfNotify = new Port<SickLrfPxy.Replace>();
            bool _correctNotificationReceived;

            [Fixture]
            public RefPlatformTests Fixture { get; set; }

		    [Start]
            public IEnumerator<ITask> Start()
			{
		        Fixture.SickLrfPort.Subscribe(_lrfNotify);
		        Fixture.TesterService.Activate(Arbiter.Receive(true, _lrfNotify, OnLrfNotification));
				yield break;
			}

            void OnLrfNotification(SickLrfPxy.Replace replace)
		    {
		        _correctNotificationReceived = CheckStateAndMeasurements(replace.Body);
		    }

		    [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				SickLrfPxy.State lrfState = null;
                yield return Arbiter.Receive<SickLrfPxy.State>(false, Fixture.SickLrfPort.Get(), ss => lrfState = ss);

			    @return(CheckStateAndMeasurements(lrfState) && _correctNotificationReceived);
		        _correctNotificationReceived = false;
			}

			bool CheckStateAndMeasurements(SickLrfPxy.State lrfState)
            {
				if (lrfState.DistanceMeasurements == null)
					return false;

                //0th measurement is on the right side of the robot, the brick is is in range, it's exactly on robot axis,
                //so the minimal distance is 4500 at 90 degrees to the left. On the left side the brick is out of lrf's range.
                // _          .       _
                //[_]   ------R------[_]
			    var minMeasurIndex = (int)((lrfState.AngularRange / 2 - 90) / lrfState.AngularResolution);
				return lrfState.DistanceMeasurements.Length == 667 &&
                        lrfState.DistanceMeasurements[minMeasurIndex].BetweenL(4499, 4501) &&
                        lrfState.DistanceMeasurements[minMeasurIndex - 5].BetweenL(4499, 4510) &&
                        lrfState.DistanceMeasurements[minMeasurIndex + 5].BetweenL(4499, 4510) &&
                        lrfState.DistanceMeasurements.Take(minMeasurIndex - 50).
                        Concat(lrfState.DistanceMeasurements.Skip(minMeasurIndex + 50)).All(d => d == 5600);
            }
		}
    }
}
