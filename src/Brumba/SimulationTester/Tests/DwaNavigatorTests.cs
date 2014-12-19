using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.Simulation.EnvironmentBuilder;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Diagnostics;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Xna.Framework;
using bPose = Brumba.WaiterStupid.Pose;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using xVector2 = Microsoft.Xna.Framework.Vector2;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using rVector2 = Microsoft.Robotics.PhysicalModel.Vector2;
using rVector3 = Microsoft.Robotics.PhysicalModel.Vector3;
using DwaNavigatorPxy = Brumba.DwaNavigator.Proxy;
using BrTimerPxy = Brumba.Entities.Timer.Proxy;

namespace Brumba.SimulationTester.Tests
{
    [SimTestFixture("dwa_navigator", Wip = true, PhysicsTimeStep = 0.002f )]
    public class DwaNavigatorTests
    {
        public SimulationTesterService TesterService { get; private set; }
        public Microsoft.Robotics.Services.Drive.Proxy.DriveOperations RefPlDrivePort { get; private set; }
        public DwaNavigatorPxy.DwaNavigatorOperations DwaNavigatorPort { get; set; }

        [SetUp]
        public void SetUp(SimulationTesterService testerService)
        {
            TesterService = testerService;
            RefPlDrivePort = testerService.ForwardTo<Microsoft.Robotics.Services.Drive.Proxy.DriveOperations>(
                    "stupid_waiter_ref_platform/differentialdrive");
            DwaNavigatorPort = testerService.ForwardTo<DwaNavigatorPxy.DwaNavigatorOperations>("dwa_navigator@");
        }

        private IEnumerator<ITask> Wait(float time)
        {
            var subscribeRq = TesterService.Timer.Subscribe(time);
            yield return To.Exec(subscribeRq.ResponsePort);
            yield return (subscribeRq.NotificationPort as BrTimerPxy.TimerOperations).P4.Receive();
            subscribeRq.NotificationShutdownPort.Post(new Shutdown());
        }

        //[SimTest(10, IsProbabilistic = false)]
        public class ClearStraightPathToTarget
        {
            [Fixture]
            public DwaNavigatorTests Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(VisualEntity entity)
            {
                entity.State.Pose.Orientation = Microsoft.Robotics.PhysicalModel.Quaternion.FromAxisAngle(0, 1, 0, -MathHelper.PiOver2);
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield return To.Exec(Fixture.DwaNavigatorPort.SetTarget(new xVector2(0, 5)));
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                @return(true);
                yield break;
            }
        }

        //[SimTest(10, IsProbabilistic = false)]
        public class ClearCurvedPathToTarget
        {
            [Fixture]
            public DwaNavigatorTests Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(VisualEntity entity)
            {
                entity.State.Pose.Orientation = Microsoft.Robotics.PhysicalModel.Quaternion.FromAxisAngle(0, 1, 0, 0);
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield return To.Exec(Fixture.DwaNavigatorPort.SetTarget(new xVector2(0, 5)));
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                @return(true);
                yield break;
            }
        }

        [SimTest(80)]
        public class DriveStraightAvoidObstacle
        {
            [Fixture]
            public DwaNavigatorTests Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(VisualEntity entity)
            {
                entity.State.Pose.Position = new rVector3(0.3f, 0, 0);
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield return To.Exec(Fixture.DwaNavigatorPort.SetTarget(new xVector2(10, 0)));
			}

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return,
                IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities,
                double elapsedTime)
            {
                @return(true);
                yield break;
            }
        }
    }
}
