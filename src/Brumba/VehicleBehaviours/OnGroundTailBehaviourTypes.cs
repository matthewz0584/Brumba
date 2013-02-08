using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.VehicleBrains.Behaviours.OnGroundTailBehaviour
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2013/01/ongroundtailbehaviour.html";
	}
	
	[DataContract]
    public class OnGroundTailBehaviourState
	{
        [DataMember]
        public float VehicleMass { get; set; }

        [DataMember]
        public float VehicleWheelBase { get; set; }

		[DataMember]
		public float VehicleWheelsSpacing { get; set; }

        [DataMember]
        public float VehicleCmHeight { get; set; }

        [DataMember]
        public float TailMass { get; set; }

        [DataMember]
        public float TailSegment1Length { get; set; }

        [DataMember]
        public float TailSegment2Length { get; set; }
    }
	
	[ServicePort]
    public class OnGroundTailBehaviourOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, Replace>
	{
	}

    public class Get : Get<GetRequestType, PortSet<OnGroundTailBehaviourState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

        public Get(GetRequestType body, PortSet<OnGroundTailBehaviourState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

    public class Replace : Replace<OnGroundTailBehaviourState, PortSet<DefaultReplaceResponseType, Fault>>
    {
        public Replace()
        {}

        public Replace(OnGroundTailBehaviourState body)
            : base(body)
        {}
    }
}