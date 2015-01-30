using Brumba.Common;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.GenericLocalizer
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/11/genericlocalizer.html";
    }

    [DataContract]
    public class GenericLocalizerState
    {
        [DataMember]
        public Pose EstimatedPose { get; set; }
    }

    [ServicePort]
    public class GenericLocalizerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    public class Get : Get<GetRequestType, PortSet<GenericLocalizerState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<GenericLocalizerState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
}