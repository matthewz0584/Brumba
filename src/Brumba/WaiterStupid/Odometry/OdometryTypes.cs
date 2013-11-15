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

	[DataContract]
	public class OdometryServiceState
	{
		[DataMember]
		public OdometryState State { get; set; }

		[DataMember]
		public OdometryConstants Constants { get; set; }
	}

	[ServicePort]
	public class OdometryOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, UpdateConstants>
	{
	}

	public class Get : Get<GetRequestType, PortSet<OdometryServiceState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

		public Get(GetRequestType body, PortSet<OdometryServiceState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

	public class UpdateConstants : Update<OdometryConstants, PortSet<DefaultUpdateResponseType, Fault>>
	{
	}
}