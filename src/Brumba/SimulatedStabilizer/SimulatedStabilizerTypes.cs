using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedStabilizer
{
	public sealed class Contract
	{
		[DataMember]
		public const string Identifier = "http://schemas.tempuri.org/2013/01/simulatedstabilizer.html";
	}
	
	[DataContract]
	public class SimulatedStabilizerState
	{
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool Connected { get; set; }
        
        [DataMember]
        [Description("")]
        public float LfWheelToGroundDistance { get; set; }
        [DataMember]
        [Description("")]
        public float RfWheelToGroundDistance { get; set; }
        [DataMember]
        [Description("")]
        public float LrWheelToGroundDistance { get; set; }
        [DataMember]
        [Description("")]
        public float RrWheelToGroundDistance { get; set; }

        [DataMember]
        [Description("Polar tail weight coordinates: angle")]
        public float TailDirection { get; set; }
        [DataMember]
        [Description("Polar tail weight coordinates: radius")]
        public float TailShoulder { get; set; }
	}
	
	[ServicePort]
    public class SimulatedStabilizerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, MoveTail, Park>
	{
	}

    public class Get : Get<GetRequestType, PortSet<SimulatedStabilizerState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

        public Get(GetRequestType body, PortSet<SimulatedStabilizerState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

    [DataContract]
    public class ParkRequest
    {
    }

    public class Park : Update<ParkRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }

    [DataContract]
    public class MoveTailRequest
    {
        [DataMember, DataMemberConstructor]
        public float Angle { get; set; }
        [DataMember, DataMemberConstructor]
        public float Shoulder { get; set; }
    }

    public class MoveTail : Update<MoveTailRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
}