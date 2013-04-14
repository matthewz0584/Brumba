using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Physics;
using TrtPxy = Brumba.Simulation.SimulatedTurret.Proxy;
using Microsoft.Ccr.Core;
using VisualEntityPxy = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;

namespace Brumba.Simulation.SimulationTester.Tests
{
    [SimulationTestFixture]
    public class TurretTests : SimulationTestFixture
    {
        public TurretTests(ServiceForwarder serviceForwarder)
            : base(new ISimulationTest[] { new SetBaseAngleTest() }, "turret", serviceForwarder)
        {
        }

        public TrtPxy.SimulatedTurretOperations TurretPort { get; set; }

        protected override void SetUpServicePorts(ServiceForwarder serviceForwarder)
        {
            TurretPort = serviceForwarder.ForwardTo<TrtPxy.SimulatedTurretOperations>("testee_turret");
        }
    }

    public class SetBaseAngleTest : DeterministicTestBase
    {
        public override IEnumerator<ITask> Start()
        {
            EstimatedTime = 3;
            (Fixture as TurretTests).TurretPort.SetBaseAngle((float) Math.PI/4);
            yield break;
        }

        public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<VisualEntityPxy> simStateEntities, double elapsedTime)
        {
            var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(simStateEntities.Single(pxy => pxy.State.Name == "turret").State.Pose.Orientation));
            @return(orientation.Y > 45 * 0.95 && orientation.Y < 45 * 1.05 && elapsedTime > EstimatedTime);
            yield break;
        }
    }
}
