using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.MapProvider;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Diagnostics;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using W3C.Soap;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using DC = System.Diagnostics.Contracts;
using MapProviderPxy = Brumba.MapProvider.Proxy;

namespace Brumba.PathPlanner
{
    [Contract(Contract.Identifier)]
    [DisplayName("Brumba Path Planner")]
    [Description("no description provided")]
    public class PathPlannerService : DsspServiceExposing
	{
#pragma warning disable 0649
		[ServiceState]
		[InitialStatePartner(Optional = false)] private PathPlannerState _state;
#pragma warning restore 0649

		[ServicePort("/PathPlanner", AllowMultipleInstances = true)]
        PathPlannerOperations _mainPort = new PathPlannerOperations();

        [Partner("Map", Contract = MapProviderPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        MapProviderPxy.MapProviderOperations _mapProvider = new MapProviderPxy.MapProviderOperations();

        PathPlanner _pathPlanner;

		public PathPlannerService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
            DC.Contract.Requires(creationPort != null);
        }

        public PathPlannerState State
        {
            get { return _state; }
        }

        protected override void Start()
        {
            DC.Contract.Assume(State != null);
            DC.Contract.Assume(State.RobotDiameter >= 0);

            SpawnIterator(StartIt);
        }

        IEnumerator<ITask> StartIt()
        {
            DC.Contract.Ensures(_pathPlanner != null);
            DC.Contract.Ensures(State.InflatedMap != null);

            OccupancyGrid map = null;
            yield return _mapProvider.Get().Receive(ms => map = (OccupancyGrid)DssTypeHelper.TransformFromProxy(ms.Map));
            map.Freeze();

            _pathPlanner = new PathPlanner(map, State.RobotDiameter);

            if (ToPlanFromState())
                _state = Plan(State.Start, State.Goal);

            base.Start();
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void InitGoalRequest(InitGoal initGoalRq)
        {
            DC.Contract.Requires(initGoalRq != null);
            DC.Contract.Requires(initGoalRq.Body != null);
            DC.Contract.Requires(initGoalRq.ResponsePort != null);
            DC.Contract.Requires(State.InflatedMap != null);
            DC.Contract.Requires(State.InflatedMap.Covers(State.Start) && !State.InflatedMap[State.Start]);
            DC.Contract.Requires(State.InflatedMap.Covers(initGoalRq.Body.Goal) && !State.InflatedMap[initGoalRq.Body.Goal]);
            DC.Contract.Ensures(State.Start == DC.Contract.OldValue(State.Start));
            DC.Contract.Ensures(State.Goal == initGoalRq.Body.Goal);
            DC.Contract.Ensures(State.InflatedMap == DC.Contract.OldValue(State.InflatedMap));
            DC.Contract.Ensures(State.RobotDiameter == DC.Contract.OldValue(State.RobotDiameter));
            DC.Contract.Assume(_pathPlanner != null);

            CompleteUpdateRequest(
                Plan(State.Start, initGoalRq.Body.Goal),
                initGoalRq.ResponsePort);
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void InitStartRequest(InitStart initStartRq)
        {
            DC.Contract.Requires(initStartRq != null);
            DC.Contract.Requires(initStartRq.Body != null);
            DC.Contract.Requires(initStartRq.ResponsePort != null);
            DC.Contract.Requires(State.InflatedMap != null);
            DC.Contract.Requires(State.InflatedMap.Covers(State.Goal) && !State.InflatedMap[State.Goal]);
            DC.Contract.Requires(State.InflatedMap.Covers(initStartRq.Body.Start) && !State.InflatedMap[initStartRq.Body.Start]);
            DC.Contract.Ensures(State.Goal == DC.Contract.OldValue(State.Goal));
            DC.Contract.Ensures(State.Start == initStartRq.Body.Start);
            DC.Contract.Ensures(State.InflatedMap == DC.Contract.OldValue(State.InflatedMap));
            DC.Contract.Ensures(State.RobotDiameter == DC.Contract.OldValue(State.RobotDiameter));
            DC.Contract.Assume(_pathPlanner != null);

            CompleteUpdateRequest(
                Plan(initStartRq.Body.Start, State.Goal),
                initStartRq.ResponsePort);
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void InitStartAndGoalRequest(InitStartAndGoal initStartAndGoalRq)
        {
            DC.Contract.Requires(initStartAndGoalRq != null);
            DC.Contract.Requires(initStartAndGoalRq.Body != null);
            DC.Contract.Requires(initStartAndGoalRq.ResponsePort != null);
            DC.Contract.Requires(State.InflatedMap != null);
            DC.Contract.Requires(State.InflatedMap.Covers(initStartAndGoalRq.Body.Goal) && !State.InflatedMap[initStartAndGoalRq.Body.Goal]);
            DC.Contract.Requires(State.InflatedMap.Covers(initStartAndGoalRq.Body.Start) && !State.InflatedMap[initStartAndGoalRq.Body.Start]);
            DC.Contract.Ensures(State.Start == initStartAndGoalRq.Body.Start);
            DC.Contract.Ensures(State.Goal == initStartAndGoalRq.Body.Goal);
            DC.Contract.Ensures(State.InflatedMap == DC.Contract.OldValue(State.InflatedMap));
            DC.Contract.Ensures(State.RobotDiameter == DC.Contract.OldValue(State.RobotDiameter));
            DC.Contract.Assume(_pathPlanner != null);

            CompleteUpdateRequest(
                Plan(initStartAndGoalRq.Body.Start, initStartAndGoalRq.Body.Goal),
                initStartAndGoalRq.ResponsePort);
        }

        PathPlannerState Plan(Vector2 start, Vector2 goal)
        {
            DC.Contract.Requires(_pathPlanner.InflatedMap != null);
            DC.Contract.Requires(_pathPlanner.InflatedMap.Covers(start) && !_pathPlanner.InflatedMap[start]);
            DC.Contract.Requires(_pathPlanner.InflatedMap.Covers(goal) && !_pathPlanner.InflatedMap[goal]);
            DC.Contract.Ensures(DC.Contract.Result<PathPlannerState>().InflatedMap == State.InflatedMap);
            DC.Contract.Ensures(DC.Contract.Result<PathPlannerState>().RobotDiameter == State.RobotDiameter);
            DC.Contract.Ensures(DC.Contract.Result<PathPlannerState>().Start == start);
            DC.Contract.Ensures(DC.Contract.Result<PathPlannerState>().Goal == goal);

            return new PathPlannerState
            {
                Start = start,
                Goal = goal,
                Path = _pathPlanner.Plan(start, goal).ToList(),
                RobotDiameter = State.RobotDiameter,
                InflatedMap = _pathPlanner.InflatedMap
            };
        }

        void CompleteUpdateRequest(PathPlannerState state, PortSet<DefaultUpdateResponseType, Fault> responsePort)
        {
            DC.Contract.Requires(responsePort != null);

            _state = state;
            responsePort.Post(new DefaultUpdateResponseType());
        }

        bool ToPlanFromState()
        {
            DC.Contract.Requires(State.InflatedMap != null);

            return State.InflatedMap.Covers(State.Goal) && State.InflatedMap.Covers(State.Start);
        }
	}
}