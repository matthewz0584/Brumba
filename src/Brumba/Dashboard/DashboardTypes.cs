using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Dashboard
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/11/dashboard.html";
	}
	
	[DataContract]
	public class DashboardState
	{
	}
	
	[ServicePort]
	public class DashboardOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}
	
	public class Get : Get<GetRequestType, PortSet<DashboardState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}
		
		public Get(GetRequestType body, PortSet<DashboardState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}