using System.Collections.Generic;
using Brumba.MapProvider;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Xna.Framework;
using W3C.Soap;

namespace Brumba.PathPlanner
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/07/pathplannerservice.html";
    }

    [DataContract]
    public class PathPlannerState
    {
		[DataMember]
		public Vector2 Start { get; set; }
		[DataMember]
        public Vector2 Goal { get; set; }
        [DataMember]
        public float RobotDiameter { get; set; }
        [DataMember]
        public List<Vector2> Path { get; set; }
        [DataMember]
        public OccupancyGrid InflatedMap { get; set; }
    }

    [ServicePort]
    public class PathPlannerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, InitStart, InitGoal, InitStartAndGoal, Replan>
    {
    }

	public class Get : Get<GetRequestType, PortSet<PathPlannerState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<PathPlannerState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    [DataContract]
    public class InitStartRequest
    {
        [DataMember, DataMemberConstructor]
        public Vector2 Start { get; set; }
    }

    public class InitStart : Update<InitStartRequest, PortSet<DefaultUpdateResponseType, Fault>>
    { }

    [DataContract]
    public class InitGoalRequest
    {
        [DataMember, DataMemberConstructor]
        public Vector2 Goal { get; set; }
    }

    public class InitGoal : Update<InitGoalRequest, PortSet<DefaultUpdateResponseType, Fault>>
    { }

    [DataContract]
    public class InitStartAndGoalRequest
    {
        [DataMember, DataMemberConstructor]
        public Vector2 Start { get; set; }
        [DataMember, DataMemberConstructor]
        public Vector2 Goal { get; set; }
    }

    public class InitStartAndGoal : Update<InitStartAndGoalRequest, PortSet<DefaultUpdateResponseType, Fault>>
    { }

    [DataContract]
    public class ReplanRequest
    {}
    
    public class Replan : Submit<ReplanRequest, PortSet<DefaultSubmitResponseType, Fault>>
    {}
}