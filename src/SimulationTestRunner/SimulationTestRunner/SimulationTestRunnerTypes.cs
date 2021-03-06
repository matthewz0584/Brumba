using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.SimulationTestRunner
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2012/11/simulationtestrunner.html";
	}
	
	[DataContract]
	public class SimulationTestRunnerState
	{
        [DataMember]
        public string TestsAssembly { get; set; }
        [DataMember]
        public string TestsDirectory { get; set; }
		[DataMember]
		public bool ToRender { get; set; }
		[DataMember]
		public bool ToDropHostOnFinish { get; set; }
        [DataMember]
        public bool FastCheck { get; set; }
	}
	
	[ServicePort]
	public class SimulationTestRunnerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}
	
	public class Get : Get<GetRequestType, PortSet<SimulationTestRunnerState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}
		
		public Get(GetRequestType body, PortSet<SimulationTestRunnerState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}


