using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Simulation;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Physics;
using VisualEntity = Microsoft.Robotics.Simulation.Engine.VisualEntity;

namespace Brumba.SimulationTester.Tests
{
    [SimTestFixture("ackerman_vehicle_ex_on_terrain03")]
    public class AckermanVehicleExTests
    {
        public AckermanVehicle.Proxy.AckermanVehicleOperations VehiclePort { get; set; }

        [SimSetUp]
		public void SetUp(SimulationTesterService testerService)
        {
            VehiclePort = testerService.ForwardTo<AckermanVehicle.Proxy.AckermanVehicleOperations>("testee_veh_service/genericackermanvehicle");
        }

        public abstract class SingleVehicleTest : StochasticTest
        {
            public const string VEHICLE_NAME = "testee@";

	        public override void PrepareForReset(VisualEntity entity)
            {
                entity.State.Pose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, (float)(2 * Math.PI * RandomG.NextDouble()));
            }
        }

        [SimTest]
        public class StraightPathTest : SingleVehicleTest
        {
            public override IEnumerator<ITask> Start()
            {
                float motorPower = 0.6f;
                EstimatedTime = 50 / (AckermanVehicles.HardRearDriven.MaxVelocity * motorPower);//50 meters
                yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(motorPower));
            }

            public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(simStateEntities.Single(entityProxy => entityProxy.State.Name == VEHICLE_NAME).State.Pose.Position));
                @return(pos.Length() > 50);
                yield break;
            }
        }

        [SimTest]
        public class CurvedPathTest : SingleVehicleTest
        {
            public override IEnumerator<ITask> Start()
            {
                EstimatedTime = 20;
                var steerAngle = RandomG.Next(0, 1) == 1 ? 0.1f : -0.1f;
                yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateSteeringAngle(steerAngle));
                yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(0.5f));
            }

            public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(simStateEntities.Single(entityProxy => entityProxy.State.Name == VEHICLE_NAME).State.Pose.Orientation));
                @return(Math.Abs(orientation.X) < 90 && elapsedTime > EstimatedTime);
                yield break;
            }
        }

        [SimTest]
        public class VehicleAnglesTest : SingleVehicleTest
        {
            float _prevDriveAngularDistance = -1;
            double _prevElapsedTime;

            public override IEnumerator<ITask> Start()
            {
                EstimatedTime = 4;
                yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateSteeringAngle(0.5f));
                yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(0.2f));
            }

            public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                if (elapsedTime < EstimatedTime / 2)
                {
                    @return(false);
                    yield break;
                }

                if (_prevDriveAngularDistance == -1)
                {
                    _prevElapsedTime = elapsedTime;
                    yield return Arbiter.Receive<AckermanVehicle.Proxy.AckermanVehicleState>(false, (Fixture as AckermanVehicleExTests).VehiclePort.Get(), vs => _prevDriveAngularDistance = vs.DriveAngularDistance);
                    @return(false);
                    yield break;
                }

                AckermanVehicle.Proxy.AckermanVehicleState vehState = null;
                yield return Arbiter.Receive<AckermanVehicle.Proxy.AckermanVehicleState>(false, (Fixture as AckermanVehicleExTests).VehiclePort.Get(), vs => vehState = vs);

                var deltaT = elapsedTime - _prevElapsedTime;
                var deltaAnglularDistance = vehState.DriveAngularDistance - _prevDriveAngularDistance;
                var vehProps = (simStateEntities.Single(entityProxy => entityProxy.State.Name == VEHICLE_NAME) as Simulation.SimulatedAckermanVehicle.Proxy.AckermanVehicleExEntity).Props;
                var expectedDeltaAngularDistance = vehProps.MaxVelocity * 0.2f / vehProps.WheelsProperties.First().Radius * deltaT;

                @return(vehState.SteeringAngle > 0.9 * 0.5 * vehProps.MaxSteeringAngle && vehState.SteeringAngle < 1.1 * 0.5 * vehProps.MaxSteeringAngle &&
                        deltaAnglularDistance > 0.9 * expectedDeltaAngularDistance && deltaAnglularDistance < 1.1 * expectedDeltaAngularDistance);
            }
        }
    }
}
