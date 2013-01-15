using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Xna.Framework;
using W3C.Soap;

namespace Brumba.VehicleBrains.Behaviours.AirborneStabilizerBehaviour
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://schemas.tempuri.org/2013/01/airbornestabilizerbehaviour.html";
	}
	
	[DataContract]
    public class AirborneStabilizerBehaviourState
	{
        [DataMember]
        [Description("")]
        public Vector3 LfRangefinderPosition { get; set; }
        [DataMember]
        [Description("")]
        public Vector3 RfRangefinderPosition { get; set; }
        [DataMember]
        [Description("")]
        public Vector3 LrRangefinderPosition { get; set; }
        [DataMember]
        [Description("")]
        public Vector3 RrRangefinderPosition { get; set; }

        [DataMember]
        [Description("Rangefinders' scan interval in ms")]
        public int ScanPeriod { get; set; }
	}
	
	[ServicePort]
    public class AirborneStabilizerBehaviourOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, Replace>
	{
	}

    public class Get : Get<GetRequestType, PortSet<AirborneStabilizerBehaviourState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

        public Get(GetRequestType body, PortSet<AirborneStabilizerBehaviourState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

    public class Replace : Replace<AirborneStabilizerBehaviourState, PortSet<DefaultReplaceResponseType, Fault>>
    {
        public Replace()
        {}

        public Replace(AirborneStabilizerBehaviourState body)
            : base(body)
        {}
    }
}