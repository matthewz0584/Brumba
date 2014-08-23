using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.MapProvider;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine.Proxy;
using Microsoft.Xna.Framework;
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
    [SimTestFixture("path_planner")]
	public class PathPlannerTests
	{
        public SimulationTesterService HostService { get; private set; }
        public PathPxy.PathPlannerOperations PathPlannerPort { get; set; }
        public MapPxy.MapProviderOperations MapProviderPort { get; set; }

	    [SetUp]
		public void SetUp(SimulationTesterService hostService)
		{
	        HostService = hostService;
	        PathPlannerPort = hostService.ForwardTo<PathPxy.PathPlannerOperations>("path_planner");
            MapProviderPort = hostService.ForwardTo<MapPxy.MapProviderOperations>("map_provider");
		}

        [SimTest(1, IsProbabilistic = false)]
        public class InitTest : ITest
        {
            [Fixture]
            public PathPlannerTests Fixture { get; set; }

            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities,
                double elapsedTime)
            {
                var initGoalRsp = Fixture.PathPlannerPort.InitGoal(new Vector2(5.6f, 1.5f));
                var initStartRsp = Fixture.PathPlannerPort.InitStart(new Vector2(0.1f, 3.5f));

                yield return Arbiter.JoinedReceive<DefaultUpdateResponseType, DefaultUpdateResponseType>(false,
                    initGoalRsp, initStartRsp,
                    (successInitStart, successInitGoal) => { });

                yield return Fixture.PathPlannerPort.Get().Choice(
                    state => @return(state.Goal == new Vector2(5.6f, 1.5f) && state.Start == new Vector2(0.1f, 3.5f)),
                    fail => @return(false));
            }
        }

        [SimTest(1, IsProbabilistic = false)]
        public class ReplanTest : ITest
        {
            [Fixture]
            public PathPlannerTests Fixture { get; set; }

            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities,
                double elapsedTime)
            {
                yield return To.Exec(Fixture.PathPlannerPort.InitStartAndGoal(new Vector2(0.15f, 3.45f), new Vector2(5.45f, 1.45f)));
                
                PortSet<PathPxy.PathPlannerState, Fault> getResponse = null;
                yield return Fixture.PathPlannerPort.Replan().Choice(
                    success => getResponse = Fixture.PathPlannerPort.Get(),
                    fail => @return(false));

                if (getResponse == null)
                    yield break;

                yield return getResponse.Choice(
                    state => @return(CheckPath(state.Path, (OccupancyGrid) DssTypeHelper.TransformFromProxy(state.InflatedMap))),
                    fail => @return(false));
            }

            bool CheckPath(List<Vector2> path, OccupancyGrid map)
            {
                map.Freeze();
                var checkPoints = path.Select(map.PosToCell).ToList();
                return path.Count == _correctCheckPoints.Count &&
                        _correctCheckPoints.Select((p, i) => new { p, i }).All(pi => checkPoints[pi.i] == pi.p);
            }

            readonly List<Point> _correctCheckPoints = new List<Point>
            {
                new Point(10, 43), new Point(21, 43), new Point(38, 26), new Point(61, 26),
                new Point(63, 24), new Point(63, 21), new Point(61, 19), new Point(59, 19), new Point(54, 14)
            };
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
                inflatedMap.Freeze();

                OccupancyGrid map = null;
                yield return Fixture.MapProviderPort.Get().Choice(
                    state => map = (OccupancyGrid)DssTypeHelper.TransformFromProxy(state.Map),
                    fail => @return(false));

                if (map == null)
                    yield break;
                map.Freeze();
                
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

                @return(occupiedInInflatedMap.Between((int)(occupiedInMap * 4.5), (int)(occupiedInMap * 5.5)));
            }
        }
	}
}