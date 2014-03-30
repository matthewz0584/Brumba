using Brumba.MapProvider;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.WaiterStupid.McLocalization
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/03/mclrflocalizerservice.html";
    }

    [DataContract]
    public class McLrfLocalizerState
    {
        [DataMember]
        public Pose FirstPoseCandidate { get; set; }

        [DataMember]
        public RangefinderProperties RangeFinderProperties { get; set; }

        [DataMember]
        public OccupancyGrid Map { get; set; }

        [DataMember]
        public int ParticlesNumber { get; set; }

        [DataMember]
        public float DeltaT { get; set; }
    }

    [ServicePort]
    public class McLrfLocalizerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, QueryPose>
    {
    }

    public class Get : Get<GetRequestType, PortSet<McLrfLocalizerState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<McLrfLocalizerState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    public class QueryPose : Query<PoseRequest, PortSet<Pose, DefaultQueryResponseType>>
    {}

    [DataContract]
    public class PoseRequest
    {}
}