using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.EnvironmentBuilder
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2012/10/environmentbuilder.html";
	}
	
	[DataContract]
    public class EnvironmentBuilderState
	{
	}
	
	[ServicePort]
    public class EnvironmentBuilderOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}

    public class Get : Get<GetRequestType, PortSet<EnvironmentBuilderState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

        public Get(GetRequestType body, PortSet<EnvironmentBuilderState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}


