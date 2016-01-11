using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Common;
using Brumba.SimulationTestRunner;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Physics;
using VisualEntity = Microsoft.Robotics.Simulation.Engine.VisualEntity;

namespace Brumba.AcceptanceTests
{
    [SimTestFixture("ackerman_vehicle_ex_on_terrain03")]
    public class AckermanVehicleExTests
    {
        static readonly Random RandomG = new Random((int)DateTime.Now.Ticks);

        public AckermanVehicle.Proxy.AckermanVehicleOperations VehiclePort { get; set; }

        [SetUp]
        public void SetUp(SimulationTestRunnerService testerService)
        {
            VehiclePort = testerService.ForwardTo<AckermanVehicle.Proxy.AckermanVehicleOperations>("testee_veh_service/genericackermanvehicle");
        }

        //50 meters
        [SimTest(50f / (4.16f * 0.6f))]
        public class StraightPathTest
        {

            [Fixture]
            public AckermanVehicleExTests Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(IEnumerable<VisualEntity> entities)
            {
                entities.Single().State.Pose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, (float)(2 * Math.PI * RandomG.NextDouble()));
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield return To.Exec(Fixture.VehiclePort.UpdateDrivePower(0.6f));
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose.Orientation));
                @return(Math.Abs(orientation.X) < 90);
                yield break;
            }
        }

        [SimTest(20)]
        public class CurvedPathTest
        {
            [Fixture]
            public AckermanVehicleExTests Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(IEnumerable<VisualEntity> entities)
            {
                entities.Single().State.Pose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, (float)(2 * Math.PI * RandomG.NextDouble()));
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                var steerAngle = RandomG.Next(0, 1) == 1 ? 0.1f : -0.1f;
                yield return To.Exec(Fixture.VehiclePort.UpdateSteeringAngle(steerAngle));
                yield return To.Exec(Fixture.VehiclePort.UpdateDrivePower(0.5f));
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose.Orientation));
                @return(Math.Abs(orientation.X) < 90);
                yield break;
            }
        }

        [SimTest(4,  IsProbabilistic = false)]
        public class VehicleAnglesTest
        {
            [Fixture]
            public AckermanVehicleExTests Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(IEnumerable<VisualEntity> entities)
            {
                entities.Single().State.Pose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, (float)(2 * Math.PI * RandomG.NextDouble()));
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield return To.Exec(Fixture.VehiclePort.UpdateSteeringAngle(0.5f));
                yield return To.Exec(Fixture.VehiclePort.UpdateDrivePower(0.2f));
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                AckermanVehicle.Proxy.AckermanVehicleState vehState = null;
                yield return Arbiter.Receive<AckermanVehicle.Proxy.AckermanVehicleState>(false, Fixture.VehiclePort.Get(), vs => vehState = vs);

                var vehProps = (simStateEntities.Single() as Simulation.SimulatedAckermanVehicle.Proxy.AckermanVehicleExEntity).Props;
                //Plus correction for linear acceleration
                var expectedAngularDistance = vehProps.MaxVelocity * 0.2f / vehProps.WheelsProperties.First().Radius * (float)elapsedTime - 9;

                @return(vehState.SteeringAngle.EqualsRelatively(0.5f * vehProps.MaxSteeringAngle, 0.1f) &&
                        vehState.DriveAngularDistance.EqualsRelatively(expectedAngularDistance, 0.1f));
            }
        }
    }
}
