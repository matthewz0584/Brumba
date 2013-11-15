using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.WaiterStupid.Odometry
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2013/11/odometryservice.html";
	}
	
	[ServicePort]
    public class OdometryOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}

    public class Get : Get<GetRequestType, PortSet<OdometryState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

		public Get(GetRequestType body, PortSet<OdometryState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}