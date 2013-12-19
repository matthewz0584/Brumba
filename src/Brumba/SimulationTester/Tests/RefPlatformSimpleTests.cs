using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Simulation.SimulatedLrf.Proxy;
using Brumba.Simulation.SimulatedReferencePlatform2011.Proxy;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using SickLrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;
using VisualEntity = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;

namespace Brumba.SimulationTester.Tests
{
    [SimTestFixture("ref_platform_simple_tests")]
    public class RefPlatformSimpleTests
    {
		public SimulationTesterService TesterService { get; private set; }
        public DrivePxy.DriveOperations RefPlDrivePort { get; private set; }
        public SickLrfPxy.SickLRFOperations SickLrfPort { get; private set; }

        [SimSetUp]
		public void SetUp(SimulationTesterService testerService)
        {
            TesterService = testerService;
            RefPlDrivePort = testerService.ForwardTo<DrivePxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
            SickLrfPort = testerService.ForwardTo<SickLrfPxy.SickLRFOperations>("stupid_waiter_lidar/sicklrf");
			SimRefPlDrivePort = testerService.ForwardTo<ReferencePlatform2011Operations>("stupid_waiter_ref_platform");
			SimSickLrfPort = testerService.ForwardTo<SimulatedLrfOperations>("stupid_waiter_lidar");
        }

	    protected ReferencePlatform2011Operations SimRefPlDrivePort { get; set; }
		protected SimulatedLrfOperations SimSickLrfPort { get; set; }

	    [SimTest]
        public class DriveForwardTest : DeterministicTest
        {
	        public override IEnumerator<ITask> Start()
            {
				var connected = false;
		        yield return Arbiter.JoinedReceive(false,
											(Fixture as RefPlatformSimpleTests).SimRefPlDrivePort.Get().P0,
											(Fixture as RefPlatformSimpleTests).SimSickLrfPort.Get().P0,
											(rs, ls) => connected = rs.Connected & ls.Connected);
				if (!connected)
					yield break;

				//Max speed = 1,6 m/s, distance 2 meters
				EstimatedTime = 2f / 1.6;

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
            Port<SickLrfPxy.Replace> _lrfNotify = new Port<SickLrfPxy.Replace>();
            bool _correctNotificationReceived;

		    public override void PrepareForReset(Microsoft.Robotics.Simulation.Engine.VisualEntity entity)
		    {
		    }

		    public override IEnumerator<ITask> Start()
			{
				var connected = false;
				yield return Arbiter.JoinedReceive(false,
											(Fixture as RefPlatformSimpleTests).SimRefPlDrivePort.Get().P0,
											(Fixture as RefPlatformSimpleTests).SimSickLrfPort.Get().P0,
											(rs, ls) => connected = rs.Connected & ls.Connected);
				if (!connected)
					yield break;

				EstimatedTime = 1;
		        (Fixture as RefPlatformSimpleTests).SickLrfPort.Subscribe(_lrfNotify);
		        (Fixture as RefPlatformSimpleTests).TesterService.Activate(Arbiter.Receive(true, _lrfNotify, OnLrfNotification));
				yield break;
			}

            private void OnLrfNotification(SickLrfPxy.Replace replace)
		    {
		        _correctNotificationReceived = CheckStateAndMeasurements(replace.Body);
		    }

		    public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.State lrfState = null;
                yield return Arbiter.Receive<SickLrfPxy.State>(false, (Fixture as RefPlatformSimpleTests).SickLrfPort.Get(), ss => lrfState = ss);

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
