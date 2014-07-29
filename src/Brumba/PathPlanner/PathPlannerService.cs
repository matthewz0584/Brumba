using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.MapProvider;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using W3C.Soap;
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
		[InitialStatePartner(Optional = false)]
        PathPlannerState _state;
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
            //DC.Contract.Ensures(_pathPlanner != null);
            //DC.Contract.Ensures(State.InflatedMap != null);
             
            OccupancyGrid map = null;
            yield return _mapProvider.Get().Receive(ms => map = (OccupancyGrid)DssTypeHelper.TransformFromProxy(ms.Map));
            map.Freeze();

            _pathPlanner = new PathPlanner(map, State.RobotDiameter);
            State.InflatedMap = _pathPlanner.InflatedMap;

            if (ToPlanFromSerializedState())
            {
                if (!CheckLocation(State.Goal) || !CheckLocation(State.Start))
                    StartFailed();

                State.Path = _pathPlanner.Plan(State.Start, State.Goal).ToList();                
            }

            base.Start();
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnInitGoal(InitGoal initGoalRq)
        {
            DC.Contract.Requires(initGoalRq != null);
            DC.Contract.Requires(initGoalRq.Body != null);
            DC.Contract.Requires(initGoalRq.ResponsePort != null);
            DC.Contract.Requires(State.InflatedMap != null);
            DC.Contract.Ensures(State.Start == DC.Contract.OldValue(State.Start));
            DC.Contract.Ensures(State.Goal == initGoalRq.Body.Goal || State.Goal == DC.Contract.OldValue(State.Goal));
            DC.Contract.Ensures(State.InflatedMap == DC.Contract.OldValue(State.InflatedMap));
            DC.Contract.Ensures(State.RobotDiameter == DC.Contract.OldValue(State.RobotDiameter));
            DC.Contract.Ensures(State.Path == DC.Contract.OldValue(State.Path));

            if (FailIfBadLocation(initGoalRq.Body.Goal, initGoalRq.ResponsePort.P1, "Goal"))
                return;

            State.Goal = initGoalRq.Body.Goal;
            initGoalRq.ResponsePort.Post(new DefaultUpdateResponseType());
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnInitStart(InitStart initStartRq)
        {
            DC.Contract.Requires(initStartRq != null);
            DC.Contract.Requires(initStartRq.Body != null);
            DC.Contract.Requires(initStartRq.ResponsePort != null);
            DC.Contract.Requires(State.InflatedMap != null);
            DC.Contract.Ensures(State.Goal == DC.Contract.OldValue(State.Goal));
            DC.Contract.Ensures(State.Start == initStartRq.Body.Start || State.Start == DC.Contract.OldValue(State.Start));
            DC.Contract.Ensures(State.InflatedMap == DC.Contract.OldValue(State.InflatedMap));
            DC.Contract.Ensures(State.RobotDiameter == DC.Contract.OldValue(State.RobotDiameter));
            DC.Contract.Ensures(State.Path == DC.Contract.OldValue(State.Path));

            if (FailIfBadLocation(initStartRq.Body.Start, initStartRq.ResponsePort.P1, "Start"))
                return;

            State.Start = initStartRq.Body.Start;
            initStartRq.ResponsePort.Post(new DefaultUpdateResponseType());
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnInitStartAndGoal(InitStartAndGoal initStartAndGoalRq)
        {
            DC.Contract.Requires(initStartAndGoalRq != null);
            DC.Contract.Requires(initStartAndGoalRq.Body != null);
            DC.Contract.Requires(initStartAndGoalRq.ResponsePort != null);
            DC.Contract.Requires(State.InflatedMap != null);
            DC.Contract.Ensures(State.Goal == initStartAndGoalRq.Body.Goal || State.Goal == DC.Contract.OldValue(State.Goal));
            DC.Contract.Ensures(State.Start == initStartAndGoalRq.Body.Start || State.Start == DC.Contract.OldValue(State.Start));
            DC.Contract.Ensures(State.InflatedMap == DC.Contract.OldValue(State.InflatedMap));
            DC.Contract.Ensures(State.RobotDiameter == DC.Contract.OldValue(State.RobotDiameter));
            DC.Contract.Ensures(State.Path == DC.Contract.OldValue(State.Path));

            if (FailIfBadLocation(initStartAndGoalRq.Body.Goal, initStartAndGoalRq.ResponsePort.P1, "Goal") ||
                FailIfBadLocation(initStartAndGoalRq.Body.Start, initStartAndGoalRq.ResponsePort.P1, "Start"))
                return;

            State.Start = initStartAndGoalRq.Body.Start;
            State.Goal = initStartAndGoalRq.Body.Goal;
            initStartAndGoalRq.ResponsePort.Post(new DefaultUpdateResponseType());
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnReplan(Replan replanRq)
        {
            DC.Contract.Requires(replanRq != null);
            DC.Contract.Requires(replanRq.Body != null);
            DC.Contract.Requires(replanRq.ResponsePort != null);
            DC.Contract.Requires(State.InflatedMap != null);
            DC.Contract.Ensures(State.Start == DC.Contract.OldValue(State.Start));
            DC.Contract.Ensures(State.Goal == DC.Contract.OldValue(State.Goal));
            DC.Contract.Ensures(State.InflatedMap == DC.Contract.OldValue(State.InflatedMap));
            DC.Contract.Ensures(State.RobotDiameter == DC.Contract.OldValue(State.RobotDiameter));
            DC.Contract.Assume(_pathPlanner != null);

            if (FailIfBadLocation(State.Goal, replanRq.ResponsePort.P1, "Goal") ||
                FailIfBadLocation(State.Start, replanRq.ResponsePort.P1, "Start"))
                return;

            State.Path = _pathPlanner.Plan(State.Start, State.Goal).ToList();
            replanRq.ResponsePort.Post(new DefaultSubmitResponseType());
        }

        bool ToPlanFromSerializedState()
        {
            DC.Contract.Requires(State != null);

            return State.Goal != State.Start;
        }

        bool CheckLocation(Vector2 location)
        {
            return State.InflatedMap.Covers(location) && !State.InflatedMap[location];
        }

        bool FailIfBadLocation(Vector2 location, Port<Fault> responceFaultPort, string locationName)
        {
            DC.Contract.Requires(responceFaultPort != null);
            DC.Contract.Requires(locationName != null);

            if (!CheckLocation(location))
                responceFaultPort.Post(new Fault { Reason = new[] { new ReasonText { Value = locationName + " is either not covered by map or occupied." } } });
            return !CheckLocation(location);
        }
	}
}