using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Ccr.Core;
using Brumba.Simulation.SimulatedAckermanFourWheels;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Physics;
using SimPxy = Microsoft.Robotics.Simulation.Proxy;
using SafwPxy = Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.Simulation.SimulationTester
{
    public class StraightPath : SimulationTest
    {
        protected override IEnumerator<ITask> Start(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            var motorPower = 1f;
            yield return To.Exec(vehiclePort.SetMotorPower(new SafwPxy.MotorPowerRequest { Value = motorPower }));
            @return(50 / (AckermanFourWheelsEntity.Builder.HardRearDriven.MaxVelocity * motorPower));//50 meters
            //@return(2);
        }

        public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
        {
            var vehEntity = simStateEntities.Single(e => e.State.Name == Fixture.ObjectsToRestore.First());
            var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(vehEntity.State.Pose.Position));
            @return(pos.Length() > 50);
            yield break;
        }
    }

    public class CurvedPath : SimulationTest
    {
        protected override IEnumerator<ITask> Start(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            var motorPower = 0.8f;
            var steerAngle = 0.1f;
            yield return To.Exec(vehiclePort.SetSteerAngle(new SafwPxy.SteerAngleRequest { Value = steerAngle }));
            yield return To.Exec(vehiclePort.SetMotorPower(new SafwPxy.MotorPowerRequest { Value = motorPower }));
            @return(10);
        }

		public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
        {
            var vehEntity = simStateEntities.Single(e => e.State.Name == Fixture.ObjectsToRestore.First());
            var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(vehEntity.State.Pose.Orientation));
            @return(Math.Abs(orientation.X) < 90 && elapsedTime > EstimatedTime);
            yield break;
        }
    }

    public class SimpleAckermanVehTests : SimulationTestFixture
    {
        public SimpleAckermanVehTests()
            : base(new SimulationTest[] { new StraightPath(), new CurvedPath() }, "SimpleAckermanVehicleOnTerrain.xml", new [] { "testee" })
        {
        }
    }

    public class Simple4x4AckermanVehTests : SimulationTestFixture
    {
        public Simple4x4AckermanVehTests()
            : base(new SimulationTest[] { new StraightPath(), new CurvedPath() }, "Simple4x4AckermanVehicleOnTerrain.xml", new [] { "testee" })
        {
        }
    }
}
