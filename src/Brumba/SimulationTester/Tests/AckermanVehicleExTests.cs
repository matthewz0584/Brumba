using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Physics;
using SafwPxy = Brumba.Simulation.SimulatedAckermanVehicle.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using AckPxy = Brumba.AckermanVehicle.Proxy;
using Quaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.Simulation.SimulationTester.Tests
{
    [SimulationTestFixture]
    public class AckermanVehicleExTests : SimulationTestFixture
    {
        public AckermanVehicleExTests(ServiceForwarder sf)
            : base(new SingleVehicleTestBase[] { new StraightPathTest(0.6f), new CurvedPathTest(0.5f), new VehicleAnglesTest() }, "ackerman_vehicle_ex_on_terrain03", sf)
        {
        }

        public AckPxy.AckermanVehicleOperations VehiclePort { get; set; }

        protected override void SetUpServicePorts(ServiceForwarder serviceForwarder)
        {
            VehiclePort = serviceForwarder.ForwardTo<AckPxy.AckermanVehicleOperations>("testee_veh_service/genericackermanvehicle");
        }
    }

    public abstract class SingleVehicleTestBase : StochasticTestBase
    {
        public const string VEHICLE_NAME = "testee";

        public override IEnumerable<EngPxy.VisualEntity> FindEntitiesToRestore(IEnumerable<EngPxy.VisualEntity> entityPxies)
        {
            return entityPxies.Where(pxy => pxy.State.Name == VEHICLE_NAME);
        }

        public override IEnumerable<VisualEntity> FindAndPrepareEntitiesForRestore(IEnumerable<VisualEntity> entities)
        {
            var testee = entities.Single(e => e.State.Name == VEHICLE_NAME);
            testee.State.Pose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, (float)(2 * Math.PI * RandomG.NextDouble()));
            return new[] { testee };
        }
    }

    public class StraightPathTest : SingleVehicleTestBase
    {
        private readonly float _motorPower;

        public StraightPathTest(float motorPower)
		{
            _motorPower = motorPower;
		}

        public override IEnumerator<ITask> Start()
        {
            EstimatedTime = 50 / (AckermanVehicles.HardRearDriven.MaxVelocity * _motorPower);//50 meters
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(_motorPower));
        }

        public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
        {
			var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(FindEntitiesToRestore(simStateEntities).First().State.Pose.Position));
            @return(pos.Length() > 50);
            yield break;
        }
    }

    public class CurvedPathTest : SingleVehicleTestBase
    {
        private readonly float _motorPower;

        public CurvedPathTest(float motorPower)
        {
            _motorPower = motorPower;
        }

        public override IEnumerator<ITask> Start()
        {
            EstimatedTime = 20;
            var steerAngle = RandomG.Next(0, 1) == 1 ? 0.1f : -0.1f;
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateSteeringAngle(steerAngle));
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(_motorPower));
        }

        public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
        {
			var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(FindEntitiesToRestore(simStateEntities).First().State.Pose.Orientation));
            @return(Math.Abs(orientation.X) < 90 && elapsedTime > EstimatedTime);
            yield break;
        }
    }

    public class VehicleAnglesTest : SingleVehicleTestBase
    {
        float _prevDriveAngularDistance = -1;
        double _prevElapsedTime;

        public override IEnumerator<ITask> Start()
        {
            EstimatedTime = 4;
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateSteeringAngle(0.5f));
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(0.2f));
        }

        public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
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

            AckPxy.AckermanVehicleState vehState = null;
            yield return Arbiter.Receive<AckermanVehicle.Proxy.AckermanVehicleState>(false, (Fixture as AckermanVehicleExTests).VehiclePort.Get(), vs => vehState = vs);

            var deltaT = elapsedTime - _prevElapsedTime;
            var deltaAnglularDistance = vehState.DriveAngularDistance - _prevDriveAngularDistance;
            var vehProps = (FindEntitiesToRestore(simStateEntities).First() as SafwPxy.AckermanVehicleExEntity).Props;
            var expectedDeltaAngularDistance = vehProps.MaxVelocity * 0.2f / vehProps.WheelsProperties.First().Radius * deltaT;

            @return(vehState.SteeringAngle > 0.9 * 0.5 * vehProps.MaxSteeringAngle && vehState.SteeringAngle < 1.1 * 0.5 * vehProps.MaxSteeringAngle &&
                    deltaAnglularDistance > 0.9 * expectedDeltaAngularDistance && deltaAnglularDistance < 1.1 * expectedDeltaAngularDistance);
        }
    }
}
