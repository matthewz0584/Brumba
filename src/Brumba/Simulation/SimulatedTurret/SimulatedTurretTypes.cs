using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedTurret
{
	public sealed class Contract
	{
		[DataMember]
		public const string Identifier = "http://brumba.ru/contracts/2013/04/simulatedturret.html";
	}
	
	[DataContract]
	public class SimulatedTurretState : IConnectable
	{
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool IsConnected { get; set; }
        
        [DataMember]
        [Description("")]
        public float BaseAngle { get; set; }
	}
	
	[ServicePort]
    public class SimulatedTurretOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, SetBaseAngle>
	{
	}

    public class Get : Get<GetRequestType, PortSet<SimulatedTurretState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

        public Get(GetRequestType body, PortSet<SimulatedTurretState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

    [DataContract]
    public class SetBaseAngleRequest
    {
        [DataMember, DataMemberConstructor]
        public float Angle { get; set; }
    }

    public class SetBaseAngle : Update<SetBaseAngleRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
}