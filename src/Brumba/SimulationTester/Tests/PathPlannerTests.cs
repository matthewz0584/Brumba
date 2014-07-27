using System;
using System.Collections.Generic;
using Brumba.MapProvider;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine.Proxy;
using W3C.Soap;
using RefPlPxy = Brumba.Simulation.SimulatedReferencePlatform2011.Proxy;
using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
using McLocalizationPxy = Brumba.McLrfLocalizer.Proxy;
using bPose = Brumba.WaiterStupid.Pose;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using BrTimerPxy = Brumba.Entities.Timer.Proxy;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using MapPxy = Brumba.MapProvider.Proxy;
using PathPxy = Brumba.PathPlanner.Proxy;

namespace Brumba.SimulationTester.Tests
{
    //[SimTestFixture("path_planner", Wip = true)]
	public class PathPlannerTests
	{
        public PathPxy.PathPlannerOperations PathPlannerPort { get; set; }
        public MapPxy.MapProviderOperations MapProviderPort { get; set; }

	    [SetUp]
		public void SetUp(SimulationTesterService hostService)
		{
            PathPlannerPort = hostService.ForwardTo<PathPxy.PathPlannerOperations>("path_planner");
            MapProviderPort = hostService.ForwardTo<MapPxy.MapProviderOperations>("map_provider");
		}

        bool CheckPath(List<Vector2> path)
        {
            throw new NotImplementedException();
        }

        [SimTest(1, IsProbabilistic = false)]
        public class InitGoalTest : ITest
        {
            [Fixture]
            public PathPlannerTests Fixture { get; set; }

            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities,
                double elapsedTime)
            {
                PortSet<PathPxy.PathPlannerState, Fault> pathPlannerGetResponse = null;
                yield return Fixture.PathPlannerPort.InitGoal(new Vector2(5.5f, 1.5f)).Choice(
                    success => pathPlannerGetResponse = Fixture.PathPlannerPort.Get(),
                    fail => @return(false));

                if (pathPlannerGetResponse == null)
                    yield break;

                yield return pathPlannerGetResponse.Choice(
                    state => @return(Fixture.CheckPath(state.Path)),
                    fail => @return(false));
            }
        }

        [SimTest(1, IsProbabilistic = false)]
        public class InitStartTest : ITest
        {
            [Fixture]
            public PathPlannerTests Fixture { get; set; }

            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities,
                double elapsedTime)
            {
                PortSet<PathPxy.PathPlannerState, Fault> pathPlannerGetResponse = null;
                yield return Fixture.PathPlannerPort.InitStart(new Vector2(5.5f, 1.5f)).Choice(
                    success => pathPlannerGetResponse = Fixture.PathPlannerPort.Get(),
                    fail => @return(false));

                if (pathPlannerGetResponse == null)
                    yield break;

                yield return pathPlannerGetResponse.Choice(
                    state =>
                    {
                        state.Path.Reverse();
                        @return(Fixture.CheckPath(state.Path));
                    },
                    fail => @return(false));
            }
        }

        [SimTest(1, IsProbabilistic = false)]
        public class InflatedMapTest : ITest
        {
            [Fixture]
            public PathPlannerTests Fixture { get; set; }

            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities,
                double elapsedTime)
            {
                OccupancyGrid inflatedMap = null;
                yield return Fixture.PathPlannerPort.Get().Choice(
                    state => inflatedMap = (OccupancyGrid) DssTypeHelper.TransformFromProxy(state.InflatedMap),
                    fail => @return(false));

                if (inflatedMap == null)
                    yield break;

                OccupancyGrid map = null;
                yield return Fixture.MapProviderPort.Get().Choice(
                    state => map = (OccupancyGrid)DssTypeHelper.TransformFromProxy(state.Map),
                    fail => @return(false));

                if (map == null)
                    yield break;

                int occupiedInInflatedMap = 0, occupiedInMap = 0;
                foreach (var cell in inflatedMap)
                {
                    occupiedInInflatedMap += inflatedMap[cell.Item1] ? 1 : 0;
                    occupiedInMap += map[cell.Item1] ? 1 : 0;
                    if (map[cell.Item1] && !inflatedMap[cell.Item1])
                    {
                        @return(false);
                        yield break;
                    }
                }

                @return(occupiedInInflatedMap.Between((int)(occupiedInMap * 2.5), occupiedInMap * 3));
            }
        }
	}
}