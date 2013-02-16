using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Brumba.Simulation.SimulatedAckermanVehicleEx;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Physics;
using SimPxy = Microsoft.Robotics.Simulation.Proxy;
using SafwPxy = Brumba.Simulation.SimulatedAckermanVehicleEx.Proxy;
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

        public IEnumerator<ITask> Start()
		{
			yield return To.Exec(Start, (double et) => EstimatedTime = et);
		}

		public abstract IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);

        protected abstract IEnumerator<ITask> Start(Action<double> @return);

		public IEnumerable<VisualEntity> PrepareEntitiesForRestore(IEnumerable<VisualEntity> entities)
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

        protected override IEnumerator<ITask> Start(Action<double> @return)
        {
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.SetMotorPower(new SafwPxy.MotorPowerRequest { Value = _motorPower }));
            @return(50 / (AckermanVehicleExEntity.Properties.HardRearDriven.MaxVelocity * _motorPower));//50 meters
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

        protected override IEnumerator<ITask> Start(Action<double> @return)
        {
            var steerAngle = _randomG.Next(0, 1) == 1 ? 0.1f : -0.1f;
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.SetSteerAngle(new SafwPxy.SteerAngleRequest { Value = steerAngle }));
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.SetMotorPower(new SafwPxy.MotorPowerRequest { Value = _motorPower }));
            @return(20);
        }

		public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
        {
			var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(FindEntitiesToRestore(simStateEntities).First().State.Pose.Orientation));
            @return(Math.Abs(orientation.X) < 90 && elapsedTime > EstimatedTime);
            yield break;
        }
    }

    [SimulationTestFixture]
	public class AckermanVehicleExTests : SimulationTestFixture
	{
	    public AckermanVehicleExTests(ServiceForwarder sf)
            : base(new SingleVehicleTest[] { new StraightPathTest(0.6f), new CurvedPathTest(0.5f) }, "ackerman_vehicle_ex_on_terrain03", sf)
		{
		}

	    public SafwPxy.SimulatedAckermanVehicleExOperations VehiclePort { get; set; }

	    protected override void SetUpServicePorts(ServiceForwarder serviceForwarder)
	    {
            VehiclePort = serviceForwarder.ForwardTo<SafwPxy.SimulatedAckermanVehicleExOperations>("testee_veh_service");
	    }
	}
}
