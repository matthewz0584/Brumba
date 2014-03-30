using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.WaiterStupid.Odometry
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2013/11/diffdriveodometryservice.html";
	}

	[DataContract]
	public class DiffDriveOdometryServiceState
	{
		[DataMember]
		public DiffDriveOdometryState State { get; set; }

		[DataMember]
		public DiffDriveOdometryConstants Constants { get; set; }

        [DataMember]
        public float DeltaT { get; set; }
	}

	[ServicePort]
	public class DiffDriveOdometryOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}

	public class Get : Get<GetRequestType, PortSet<DiffDriveOdometryServiceState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

		public Get(GetRequestType body, PortSet<DiffDriveOdometryServiceState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}