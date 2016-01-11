using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.SimulationTestRunner;
using Brumba.Utils;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine.Proxy;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Ccr.Core;

namespace Brumba.AcceptanceTests
{
    [SimTestFixture("turret")]
    public class TurretTests
    {
        public Simulation.SimulatedTurret.Proxy.SimulatedTurretOperations TurretPort { get; set; }

        [SetUp]
        public void SetUp(SimulationTestRunnerService hostService)
        {
            TurretPort = hostService.ForwardTo<Simulation.SimulatedTurret.Proxy.SimulatedTurretOperations>("testee_turret");
        }

        [SimTest(3, IsProbabilistic = false)]
        public class SetBaseAngleTest
        {
            [Fixture]
            public TurretTests Fixture { get; set; }

            [Start]
            public IEnumerator<ITask> Start()
            {
                Fixture.TurretPort.SetBaseAngle((float)Math.PI / 4);
                yield break;
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
            {
                var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(simStateEntities.Single().State.Pose.Orientation));
                @return(orientation.Y.EqualsRelatively(45, 0.05f));
                yield break;
            }
        }
    }
}
