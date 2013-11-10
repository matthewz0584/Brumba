using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using drivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using lrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using VisualEntityPxy = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.Simulation.SimulationTester.Tests
{
    [SimTestFixture("ref_platform_simple_tests")]
    public class RefPlatformSimpleTests
    {
        public ServiceForwarder ServiceForwarder { get; private set; }
        public drivePxy.DriveOperations RefPlDrivePort { get; private set; }
		public lrfPxy.SickLRFOperations SickLrfPort { get; private set; }

        [SimSetUp]
        public void SetUp(ServiceForwarder serviceForwarder)
        {
            ServiceForwarder = serviceForwarder;
            RefPlDrivePort = serviceForwarder.ForwardTo<drivePxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
			SickLrfPort = serviceForwarder.ForwardTo<lrfPxy.SickLRFOperations>("stupid_waiter_lidar/sicklrf");
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

			public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(simStateEntities.Single(epxy => epxy.State.Name == "stupid_waiter").State.Pose.Position));
				@return(pos.Length() > 2);
				yield break;
			}
        }

		[SimTest]
		public class LrfTest : StochasticTest
		{
            Port<lrfPxy.Replace> _lrfNotify = new Port<lrfPxy.Replace>();
            bool _correctNotificationReceived;

		    public override bool NeedResetOnEachTry(VisualEntityPxy entityProxy)
		    {
                return entityProxy.State.Name == "stupid_waiter";
		    }

		    public override void PrepareForReset(VisualEntity entity)
		    {
		    }

		    public override IEnumerator<ITask> Start()
			{
				EstimatedTime = 1;
		        (Fixture as RefPlatformSimpleTests).SickLrfPort.Subscribe(_lrfNotify);
		        (Fixture as RefPlatformSimpleTests).ServiceForwarder.Activate(Arbiter.Receive(true, _lrfNotify, OnLrfNotification));
				yield break;
			}

		    private void OnLrfNotification(lrfPxy.Replace replace)
		    {
		        _correctNotificationReceived = CheckStateAndMeasurements(replace.Body);
		    }

		    public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
			{
				lrfPxy.State lrfState = null;
				yield return Arbiter.Receive<lrfPxy.State>(false, (Fixture as RefPlatformSimpleTests).SickLrfPort.Get(), ss => lrfState = ss);

			    @return(CheckStateAndMeasurements(lrfState) && _correctNotificationReceived);
		        _correctNotificationReceived = false;
			}

            bool CheckStateAndMeasurements(lrfPxy.State lrfState)
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
