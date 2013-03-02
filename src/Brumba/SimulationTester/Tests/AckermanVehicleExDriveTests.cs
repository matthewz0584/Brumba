using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Physics;
using SafwPxy = Brumba.Simulation.SimulatedAckermanVehicleEx.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using AckPxy = Brumba.AckermanVehicle.Proxy;

namespace Brumba.Simulation.SimulationTester
{
    public class StraightPathTest : SingleVehicleTest
    {
		public StraightPathTest(float motorPower)
            : base(motorPower)
    	{}

        public override IEnumerator<ITask> Start()
        {
            EstimatedTime = 50 / (AckermanVehicles.HardRearDriven.MaxVelocity * MotorPower);//50 meters
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(MotorPower));
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
        public CurvedPathTest(float motorPower)
            : base(motorPower)
    	{}

        public override IEnumerator<ITask> Start()
        {
            EstimatedTime = 20;
            var steerAngle = _randomG.Next(0, 1) == 1 ? 0.1f : -0.1f;
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateSteerAngle(steerAngle));
            yield return To.Exec((Fixture as AckermanVehicleExTests).VehiclePort.UpdateDrivePower(MotorPower));
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

        public AckPxy.AckermanVehicleOperations VehiclePort { get; set; }

	    protected override void SetUpServicePorts(ServiceForwarder serviceForwarder)
	    {
            VehiclePort = serviceForwarder.ForwardTo<AckPxy.AckermanVehicleOperations>("testee_veh_service/genericackermanvehicle");
	    }
	}
}
