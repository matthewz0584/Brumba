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
		public const string Identifier = "http://schemas.tempuri.org/2012/10/simulatedackermanfourwheels.html";
	}
	
	[DataContract]
	public class SimulatedStabilizerState
	{
	}
	
	[ServicePort]
    public class SimulatedStabilizerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
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
}