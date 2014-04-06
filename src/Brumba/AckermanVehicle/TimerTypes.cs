using Microsoft.Dss.Core.Attributes;
using System.ComponentModel;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Ccr.Core;
using W3C.Soap;

namespace Brumba.Entities.Timer
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/04/timer.html";
    }

    [DataContract]
    public class TimerState
    {
        [DataMember]
        [Description("Simulation time")]
        public double Time { get; set; }

        [DataMember]
        [Description("Time elapsed since last tick")]
        public double Delta { get; set; }
    }

    [ServicePort]
    public class TimerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, Subscribe, Update, Pause>
    {
    }

    public class Get : Get<GetRequestType, PortSet<TimerState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<TimerState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    [DataContract]
    public class SubscribeRequest : SubscribeRequestType
    {
        [DataMember, DataMemberConstructor]
        public float Interval { get; set; }

        public override object Clone()
        {
            var cloned = base.Clone() as SubscribeRequest;
            cloned.Interval = Interval;
            return cloned;
        }
    }

    public class Subscribe : Subscribe<SubscribeRequest, PortSet<SubscribeResponseType, Fault>>
    {
        public Subscribe()
        {
        }

        public Subscribe(SubscribeRequest body)
            : base(body)
        {
        }

        public Subscribe(SubscribeRequest body, PortSet<SubscribeResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    public class Update : Update<TimerState, PortSet<DefaultUpdateResponseType, Fault>>
    {
        public Update()
        {
        }

        public Update(TimerState state)
        {
            Body = state;
        }
    }
    
    [DataContract]
    public class PauseRequest
    {
        [DataMember, DataMemberConstructor]
        public bool Pause { get; set; }
    }

    public class Pause : Update<PauseRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
}