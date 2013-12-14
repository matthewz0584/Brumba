using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Replace = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Replace;
using VisualEntity = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;

namespace Brumba.SimulationTester.Tests
{
    [SimTestFixture("ref_platform_simple_tests")]
    public class RefPlatformSimpleTests
    {
		public SimulationTesterService TesterService { get; private set; }
        public Microsoft.Robotics.Services.Drive.Proxy.DriveOperations RefPlDrivePort { get; private set; }
		public Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.SickLRFOperations SickLrfPort { get; private set; }

        [SimSetUp]
		public void SetUp(SimulationTesterService testerService)
        {
            TesterService = testerService;
            RefPlDrivePort = testerService.ForwardTo<Microsoft.Robotics.Services.Drive.Proxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			SickLrfPort = testerService.ForwardTo<Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.SickLRFOperations>("stupid_waiter_lidar/sicklrf");
        }

        [SimTest]
        public class DriveForwardTest : DeterministicTest
        {
	        public override IEnumerator<ITask> Start()
            {
				//Max speed = 1,6 m/s, distance 2 meters
				EstimatedTime = 2 * 2 / 1.6;

                //Execs for synchronization, otherwise set power message can arrive before enable message
                yield return To.Exec((Fixture as RefPlatformSimpleTests).RefPlDrivePort.EnableDrive(true));
                yield return To.Exec((Fixture as RefPlatformSimpleTests).RefPlDrivePort.SetDrivePower(1.0, 1.0));
            }

			public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose.Position));
				@return(pos.Length() > 2);
				yield break;
			}
        }

		[SimTest]
		public class LrfTest : StochasticTest
		{
            Port<Replace> _lrfNotify = new Port<Replace>();
            bool _correctNotificationReceived;

		    public override void PrepareForReset(Microsoft.Robotics.Simulation.Engine.VisualEntity entity)
		    {
		    }

		    public override IEnumerator<ITask> Start()
			{
				EstimatedTime = 1;
		        (Fixture as RefPlatformSimpleTests).SickLrfPort.Subscribe(_lrfNotify);
		        (Fixture as RefPlatformSimpleTests).TesterService.Activate(Arbiter.Receive(true, _lrfNotify, OnLrfNotification));
				yield break;
			}

		    private void OnLrfNotification(Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Replace replace)
		    {
		        _correctNotificationReceived = CheckStateAndMeasurements(replace.Body);
		    }

		    public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.State lrfState = null;
				yield return Arbiter.Receive<Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.State>(false, (Fixture as RefPlatformSimpleTests).SickLrfPort.Get(), ss => lrfState = ss);

			    @return(CheckStateAndMeasurements(lrfState) && _correctNotificationReceived);
		        _correctNotificationReceived = false;
			}

            bool CheckStateAndMeasurements(Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.State lrfState)
            {
				if (lrfState.DistanceMeasurements == null)
					return false;

                //0th measurement is on the right side of the robot, the brick is out of lrf's range on its side,
                //on the left side the brick is in range, it's exactly on robot axis, so the minimal distance is 4500 at 90 degrees to the left
                // _       .              _
                //[_]------R--------     [_]
			    var minMeasurIndex = lrfState.DistanceMeasurements.Length -
			                         (int)((lrfState.AngularRange / 2 - 90) / lrfState.AngularResolution);
				return lrfState.DistanceMeasurements.Length == 667 &&
                        lrfState.DistanceMeasurements[minMeasurIndex] == 4500 &&
                        lrfState.DistanceMeasurements[minMeasurIndex - 5] > 4500 && lrfState.DistanceMeasurements[minMeasurIndex - 5] < 4510 &&
                        lrfState.DistanceMeasurements[minMeasurIndex + 5] > 4500 && lrfState.DistanceMeasurements[minMeasurIndex + 5] < 4510 &&
                        lrfState.DistanceMeasurements.Take(minMeasurIndex - 50).
                        Concat(lrfState.DistanceMeasurements.Skip(minMeasurIndex + 50)).All(d => d == 5600);
            }
		}
    }
}
