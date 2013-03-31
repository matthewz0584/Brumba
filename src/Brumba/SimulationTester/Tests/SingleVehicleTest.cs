using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.Simulation.SimulationTester
{
    public abstract class SingleVehicleTest : ISimulationTest
    {
        public const string VEHICLE_NAME = "testee";

        protected static readonly Random _randomG = new Random((int)DateTime.Now.Ticks);	    

        public double EstimatedTime { get; protected set; }
        public ISimulationTestFixture Fixture { get; set; }

        public IEnumerable<VisualEntity> FindEntitiesToRestore(IEnumerable<VisualEntity> entityPxies)
        {
            return entityPxies.Where(pxy => pxy.State.Name == VEHICLE_NAME);
        }

        public IEnumerable<Microsoft.Robotics.Simulation.Engine.VisualEntity> FindAndPrepareEntitiesForRestore(IEnumerable<Microsoft.Robotics.Simulation.Engine.VisualEntity> entities)
        {
            var testee = entities.Single(e => e.State.Name == VEHICLE_NAME);
            testee.State.Pose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, (float)(2 * Math.PI * _randomG.NextDouble()));
            return new[] { testee };
        }

        public abstract IEnumerator<ITask> Start();
        public abstract IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime);
    }
}