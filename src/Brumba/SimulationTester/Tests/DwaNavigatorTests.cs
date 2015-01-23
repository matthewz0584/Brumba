using System;
using System.Collections.Generic;
using Brumba.DsspUtils;
using Brumba.Simulation;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
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
    [SimTestFixture("dwa_navigator", PhysicsTimeStep = 0.005f )]
    public class DwaNavigatorTests
    {
        public DwaNavigatorPxy.DwaNavigatorOperations DwaNavigatorPort { get; set; }

        [SetUp]
        public void SetUp(SimulationTesterService testerService)
        {
            DwaNavigatorPort = testerService.ForwardTo<DwaNavigatorPxy.DwaNavigatorOperations>("dwa_navigator@");
        }

        //[SimTest(6.1f, IsProbabilistic = false)]
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
                var simPosition = DiffDriveOdometryTests.ExtractPose(simStateEntities).Position.SimToMap();
                @return((new xVector2(0, 5) - simPosition).Length().AlmostEqualWithError(0, 0.1));
                yield break;
            }
        }

        //[SimTest(6.1f, IsProbabilistic = false)]
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
                var simPosition = DiffDriveOdometryTests.ExtractPose(simStateEntities).Position.SimToMap();
                @return((new xVector2(0, 5) - simPosition).Length().AlmostEqualWithError(0, 0.1));
                yield break;
            }
        }

        [SimTest(7, IsProbabilistic = false)]
        public class AvoidingObstacleStraightPath
        {
            [Fixture]
            public DwaNavigatorTests Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(VisualEntity entity)
            {
                entity.State.Pose.Position = new rVector3(0, 0, 2);
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield return To.Exec(Fixture.DwaNavigatorPort.SetTarget(new xVector2(8, 0)));
			}

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                var simPosition = DiffDriveOdometryTests.ExtractPose(simStateEntities).Position.SimToMap();
                @return((new xVector2(8, 0) - simPosition).Length().AlmostEqualWithError(0, 0.1));
                yield break;
            }
        }

        //[SimTest(7, IsProbabilistic = false)]
        public class AvoidingObstacleCurvedPath
        {
            [Fixture]
            public DwaNavigatorTests Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(VisualEntity entity)
            {
                entity.State.Pose.Position = new rVector3(0.3f, 0, 3.5f);
                entity.State.Pose.Orientation = Microsoft.Robotics.PhysicalModel.Quaternion.FromAxisAngle(0, 1, 0, -MathHelper.PiOver2);
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield return To.Exec(Fixture.DwaNavigatorPort.SetTarget(new xVector2(8, 0)));
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                var simPosition = DiffDriveOdometryTests.ExtractPose(simStateEntities).Position.SimToMap();
                @return((new xVector2(8, 0) - simPosition).Length().AlmostEqualWithError(0, 0.1));
                yield break;
            }
        }

        [SimTest(5, IsProbabilistic = false)]
        public class TargetBehindTheDoor
        {
            [Fixture]
            public DwaNavigatorTests Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(VisualEntity entity)
            {
                entity.State.Pose.Position = new rVector3(0, 0, -3.5f);
                entity.State.Pose.Orientation = Microsoft.Robotics.PhysicalModel.Quaternion.FromAxisAngle(0, 1, 0, 0);
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield return To.Exec(Fixture.DwaNavigatorPort.SetTarget(new xVector2(-5, 2)));
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                var simPosition = DiffDriveOdometryTests.ExtractPose(simStateEntities).Position.SimToMap();
                @return((new xVector2(-5, 2) - simPosition).Length().AlmostEqualWithError(0, 0.1));
                yield break;
            }
        }
    }
}
