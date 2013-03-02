using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.AckermanVehicle
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2013/02/genericackermanvehicle.html";
    }

    [DataContract]
    public class AckermanVehicleState
    {
        [DataMember]
        public int SteerMotorTicks { get; set; }

        [DataMember]
        public int DriveMotorTicks { get; set; }
    }

    [ServicePort]
    public class AckermanVehicleOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, UpdateDrivePower, UpdateSteerAngle, Break>
    {
    }

    public class Get : Get<GetRequestType, PortSet<AckermanVehicleState, Fault>>
    {
        public Get()
        {}

        public Get(GetRequestType body)
            : base(body)
        {}

        public Get(GetRequestType body, PortSet<AckermanVehicleState, Fault> responsePort)
            : base(body, responsePort)
        {}
    }

    [DataContract]
    public class DrivePower
    {
        [DataMember, DataMemberConstructor]
        public float Value { get; set; }
    }

    [DataContract]
    public class SteerAngle
    {
        [DataMember, DataMemberConstructor]
        public float Value { get; set; }
    }

    [DataContract]
    public class BreakRequest
    {
    }

    public class UpdateDrivePower : Update<DrivePower, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }

    public class UpdateSteerAngle : Update<SteerAngle, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }

    public class Break : Update<BreakRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
}
