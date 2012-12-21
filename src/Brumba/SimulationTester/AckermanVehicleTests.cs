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
	public abstract class SingleVehicleTest : ISimulationTest
	{
		public const string VEHICLE_NAME = "testee";

		protected static readonly Random _randomG = new Random((int)DateTime.Now.Ticks);

		public double EstimatedTime { get; protected set; }
		public ISimulationTestFixture Fixture { get; set; }

		public IEnumerable<EngPxy.VisualEntity> FindEntitiesToRestore(IEnumerable<EngPxy.VisualEntity> entityPxies)
		{
			return entityPxies.Where(pxy => pxy.State.Name == VEHICLE_NAME);
		}

		public IEnumerator<ITask> Start(SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
		{
			yield return To.Exec(Start, (double et) => EstimatedTime = et, vehiclePort);
		}

		public abstract IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);

		protected abstract IEnumerator<ITask> Start(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort);

		public IEnumerable<VisualEntity> PrepareEntitiesToRestore(IEnumerable<VisualEntity> entities)
		{
			var testee = entities.Single(e => e.State.Name == VEHICLE_NAME);
			testee.State.Pose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, (float)(2 * Math.PI * _randomG.NextDouble()));
			return new[] { testee };
		}
	}

    public class StraightPathTest : SingleVehicleTest
    {
    	private float _motorPower;

		public StraightPathTest(float motorPower)
    	{
    		_motorPower = motorPower;
    	}

        protected override IEnumerator<ITask> Start(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            yield return To.Exec(vehiclePort.SetMotorPower(new SafwPxy.MotorPowerRequest { Value = _motorPower }));
            @return(50 / (AckermanFourWheelsEntity.Builder.HardRearDriven.MaxVelocity * _motorPower));//50 meters
            //@return(2);
        }

    	public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
        {
			var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(FindEntitiesToRestore(simStateEntities).First().State.Pose.Position));
            @return(pos.Length() > 50);
            yield break;
        }
    }

    public class CurvedPathTest : SingleVehicleTest
    {
    	private float _motorPower;

    	public CurvedPathTest(float motorPower)
    	{
    		_motorPower = motorPower;
    	}

    	protected override IEnumerator<ITask> Start(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            var steerAngle = _randomG.Next(0, 1) == 1 ? 0.1f : -0.1f;
            yield return To.Exec(vehiclePort.SetSteerAngle(new SafwPxy.SteerAngleRequest { Value = steerAngle }));
            yield return To.Exec(vehiclePort.SetMotorPower(new SafwPxy.MotorPowerRequest { Value = _motorPower }));
            @return(20);
        }

		public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
        {
			var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(FindEntitiesToRestore(simStateEntities).First().State.Pose.Orientation));
            @return(Math.Abs(orientation.X) < 90 && elapsedTime > EstimatedTime);
            yield break;
        }
    }

	public class HardRearDrivenVehicleTests : SimulationTestFixture
	{
		public HardRearDrivenVehicleTests()
			: base(new SingleVehicleTest[] { new StraightPathTest(0.6f), new CurvedPathTest(0.5f) }, "hard_rear_driven_on_terrain03.xml")
		{
		}
	}

	public class SuspendedRearDrivenVehicleTests : SimulationTestFixture
	{
		public SuspendedRearDrivenVehicleTests()
			: base(new SingleVehicleTest[] { new StraightPathTest(0.5f), new CurvedPathTest(0.45f) }, "suspended_rear_driven_on_terrain03.xml")
		{
		}
	}

	public class Hard4x4VehicleTests : SimulationTestFixture
	{
		public Hard4x4VehicleTests()
			: base(new SingleVehicleTest[] { new StraightPathTest(0.6f), new CurvedPathTest(0.5f) }, "hard_4x4_on_terrain03.xml")
		{
		}
	}

	public class Suspended4x4VehicleTests : SimulationTestFixture
	{
		public Suspended4x4VehicleTests()
			: base(new SingleVehicleTest[] { new StraightPathTest(0.5f), new CurvedPathTest(0.45f) }, "suspended_4x4_on_terrain03.xml")
		{
		}
	}
}
