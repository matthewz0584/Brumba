using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.GenericFixedWheelVelocimeter
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/11/genericfixedwheelvelocimeter.html";
    }

    [DataContract]
    public class GenericFixedWheelVelocimeterState
    {
        [DataMember]
        public Velocity Velocity { get; set; }
    }

    [ServicePort]
    public class GenericFixedWheelVelocimeterOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    public class Get : Get<GetRequestType, PortSet<GenericFixedWheelVelocimeterState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<GenericFixedWheelVelocimeterState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
}