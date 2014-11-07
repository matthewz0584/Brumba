using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.GenericLocalizer
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/11/genericposeestimator.html";
    }

    [DataContract]
    public class GenericLocalizerState
    {
        [DataMember]
        public Pose EstimatedPose { get; set; }
    }

    [ServicePort]
    public class GenericLocalizerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    public class Get : Get<GetRequestType, PortSet<GenericLocalizerState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<GenericLocalizerState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

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