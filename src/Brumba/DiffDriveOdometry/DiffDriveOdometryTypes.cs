using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.DiffDriveOdometry
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
        public Pose Pose { get; set; }
        [DataMember]
        public Velocity Velocity { get; set; }
        [DataMember]
        public int LeftTicks { get; set; }
        [DataMember]
        public int RightTicks { get; set; }

        [DataMember]
        public float WheelRadius { get; set; }
        [DataMember]
        public float WheelBase { get; set; }
        [DataMember]
        public int TicksPerRotation { get; set; }
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