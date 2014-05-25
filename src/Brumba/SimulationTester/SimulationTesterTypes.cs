using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.SimulationTester
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2012/11/simulationtester.html";
	}
	
	[DataContract]
	public class SimulationTesterState
	{
		[DataMember]
		public bool ToRender { get; set; }
		[DataMember]
		public bool ToDropHostOnFinish { get; set; }
        [DataMember]
        public bool FastCheck { get; set; }
	}
	
	[ServicePort]
	public class SimulationTesterOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}
	
	public class Get : Get<GetRequestType, PortSet<SimulationTesterState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}
		
		public Get(GetRequestType body, PortSet<SimulationTesterState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}


