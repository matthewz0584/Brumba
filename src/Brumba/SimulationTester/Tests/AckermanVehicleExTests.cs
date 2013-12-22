using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Simulation;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.PhysicalModel;
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
                EstimatedTime = (50 / (AckermanVehicles.HardRearDriven.MaxVelocity * motorPower));//50 meters
                yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(motorPower));
            }

            public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose.Orientation));
                @return(Math.Abs(orientation.X) < 90);
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
                var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose.Orientation));
                @return(Math.Abs(orientation.X) < 90);
                yield break;
            }
        }

        [SimTest]
        public class VehicleAnglesTest : SingleVehicleTest
        {
            public override IEnumerator<ITask> Start()
            {
                EstimatedTime = 4;
                yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateSteeringAngle(0.5f));
                yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(0.2f));
            }

            public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                AckermanVehicle.Proxy.AckermanVehicleState vehState = null;
                yield return Arbiter.Receive<AckermanVehicle.Proxy.AckermanVehicleState>(false, (Fixture as AckermanVehicleExTests).VehiclePort.Get(), vs => vehState = vs);

                var vehProps = (simStateEntities.Single() as Simulation.SimulatedAckermanVehicle.Proxy.AckermanVehicleExEntity).Props;
                //Plus correction for linear acceleration
                var expectedAngularDistance = vehProps.MaxVelocity * 0.2f / vehProps.WheelsProperties.First().Radius * elapsedTime - 9;

                @return(vehState.SteeringAngle > 0.9 * 0.5 * vehProps.MaxSteeringAngle && vehState.SteeringAngle < 1.1 * 0.5 * vehProps.MaxSteeringAngle &&
                        vehState.DriveAngularDistance > 0.9 * expectedAngularDistance && vehState.DriveAngularDistance < 1.1 * expectedAngularDistance);
            }
        }
    }
}
