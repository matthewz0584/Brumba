using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.GenericVelocimeter
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/11/genericvelocimeter.html";
    }

    [DataContract]
    public class GenericVelocimeterState
    {
        [DataMember]
        public Pose Velocity { get; set; }
    }

    [ServicePort]
    public class GenericVelocimeterOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    public class Get : Get<GetRequestType, PortSet<GenericVelocimeterState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<GenericVelocimeterState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
}