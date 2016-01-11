using System.Collections.Generic;
using System.ComponentModel;
using Brumba.Simulation.Common;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedTail
{
	public sealed class Contract
	{
		[DataMember]
		public const string Identifier = "http://brumba.ru/contracts/2013/01/simulatedtail.html";
	}
	
	[DataContract]
	public class SimulatedTailState : IConnectable
	{
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool IsConnected { get; set; }
        
        [DataMember]
        [Description("Ground rangefinders measurements clockwise from left front one")]
        public List<float> WheelToGroundDistances { get; set; }

        [DataMember]
        [Description("")]
        public float Segment1Angle { get; set; }
        [DataMember]
        [Description("")]
        public float Segment2Angle { get; set; }
	}
	
	[ServicePort]
    public class SimulatedTailOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, ChangeSegment1Angle, ChangeSegment2Angle, Park>
	{
	}

    public class Get : Get<GetRequestType, PortSet<SimulatedTailState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

        public Get(GetRequestType body, PortSet<SimulatedTailState, Fault> responsePort)
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
    public class ChangeSegment1AngleRequest
    {
        [DataMember, DataMemberConstructor]
        public float Angle { get; set; }
    }

    [DataContract]
    public class ChangeSegment2AngleRequest
    {
        [DataMember, DataMemberConstructor]
        public float Angle { get; set; }
    }

    public class ChangeSegment1Angle : Update<ChangeSegment1AngleRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }

    public class ChangeSegment2Angle : Update<ChangeSegment2AngleRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
}