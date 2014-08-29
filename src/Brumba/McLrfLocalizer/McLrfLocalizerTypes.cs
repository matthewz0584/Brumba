using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.McLrfLocalizer
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
        public int ParticlesNumber { get; set; }

        [DataMember]
        public int BeamsNumber { get; set; }

        [DataMember]
        public float DeltaT { get; set; }
    }

    [ServicePort]
	public class McLrfLocalizerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, QueryPose, InitPose, InitPoseUnknown, Subscribe>
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

    [DataContract]
    public class PoseRequest
    {}

    public class QueryPose : Query<PoseRequest, PortSet<Pose, DefaultQueryResponseType>>
    {}

	[DataContract]
	public class InitPoseRequest
	{
		[DataMember, DataMemberConstructor]
		public Pose Pose { get; set; }
	}

	public class InitPose : Update<InitPoseRequest, PortSet<DefaultUpdateResponseType, Fault>>
	{}

	[DataContract]
	public class InitPoseUnknownRequest
	{}

    public class InitPoseUnknown : Update<InitPoseUnknownRequest, PortSet<DefaultUpdateResponseType, Fault>>
	{}

	[DataContract]
	public class SubscribeRequest : SubscribeRequestType
	{
	}

	public class Subscribe : Subscribe<SubscribeRequest, PortSet<SubscribeResponseType, Fault>>
	{
		public Subscribe()
		{
		}

		public Subscribe(SubscribeRequest body)
			: base(body)
		{
		}

		public Subscribe(SubscribeRequest body, PortSet<SubscribeResponseType, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}