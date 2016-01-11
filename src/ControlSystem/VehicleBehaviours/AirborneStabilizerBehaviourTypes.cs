using System.Collections.Generic;
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
        public const string Identifier = "http://brumba.ru/contracts/2013/01/airbornestabilizerbehaviour.html";
	}
	
	[DataContract]
    public class AirborneStabilizerBehaviourState
	{
        [DataMember]
        [Description("Ground rangefinders positions clockwise from front left one")]
        public List<Vector3> GroundRangefinderPositions { get; set; }

        [DataMember]
        [Description("Rangefinders' scan interval in s")]
        public float ScanInterval { get; set; }

        [DataMember]
        [Description("PID proportionality coefficient")]
        public float Kp { get; set; }
        [DataMember]
        [Description("PID derivative term time value")]
        public float Td { get; set; }

        [DataMember]
        [Description("Tail angle deadband")]
        public float TailAngleDeadband { get; set; }
        [DataMember]
        [Description("Tail shoulder deadband")]
        public float TailShoulderDeadband { get; set; }
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