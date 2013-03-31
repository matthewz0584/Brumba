using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedInfraredRfRing
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2013/03/simulatedinfraredrfring.html";
	}
	
	[DataContract]
    public class SimulatedInfraredRfRingState
	{
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool Connected { get; set; }
        
        [DataMember]
        [Description("Rangefinders measurements, order as given to entity")]
        public List<float> Distances { get; set; }
	}
	
	[ServicePort]
    public class SimulatedInfraredRfRingOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}

    public class Get : Get<GetRequestType, PortSet<SimulatedInfraredRfRingState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

        public Get(GetRequestType body, PortSet<SimulatedInfraredRfRingState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}