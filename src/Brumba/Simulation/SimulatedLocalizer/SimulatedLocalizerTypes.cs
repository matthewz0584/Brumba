using System.ComponentModel;
using Brumba.GenericLocalizer;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedLocalizer
{
	public sealed class Contract
	{
		[DataMember]
		public const string Identifier = "http://brumba.ru/contracts/2014/11/simulatedlocalizer.html";
	}
	
	[DataContract]
	public class SimulatedLocalizerState : GenericLocalizerState, IConnectable
	{
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool IsConnected { get; set; }
	}
	
	[ServicePort]
    public class SimulatedLocalizerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}

    public class Get : Get<GetRequestType, PortSet<SimulatedLocalizerState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

        public Get(GetRequestType body, PortSet<SimulatedLocalizerState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}