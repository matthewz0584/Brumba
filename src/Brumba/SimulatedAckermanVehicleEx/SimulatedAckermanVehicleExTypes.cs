using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedAckermanVehicleEx
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2012/10/simulatedackermanvehicleex.html";
	}
	
	[DataContract]
	public class SimulatedAckermanVehicleExState
	{
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool Connected { get; set; }

        [DataMember]
        public float MotorPower { get; set; }

        [DataMember]
        public float SteerAngle { get; set; }

        [DataMember]
        public float Velocity { get; set; }

        [DataMember]
        public float ActualSteerAngle { get; set; }
	}
	
	[ServicePort]
    public class SimulatedAckermanVehicleExOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, SetMotorPower, SetSteerAngle, Break>
	{
	}
	
	public class Get : Get<GetRequestType, PortSet<SimulatedAckermanVehicleExState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}
		
		public Get(GetRequestType body, PortSet<SimulatedAckermanVehicleExState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

    [DataContract]
    public class ControlRequest
    {
        [DataMember, DataMemberConstructor]
        public float Value { get; set; }
    }

    [DataContract]
    public class MotorPowerRequest : ControlRequest
    {
    }

    [DataContract]
    public class SteerAngleRequest : ControlRequest
    {
    }

    [DataContract]
    public class BreakRequest
    {
    }

    public class SetMotorPower : Update<MotorPowerRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }

    public class SetSteerAngle : Update<SteerAngleRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }

    public class Break : Update<BreakRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
}