using System.ComponentModel;
using Brumba.Entities.Timer;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedTimer
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2012/10/simulatedtimer.html";
    }

    [DataContract]
    public class SimulatedTimerState : TimerState, IConnectable
    {
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool IsConnected { get; set; }
    }

    [ServicePort]
    public class SimulatedTimerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    public class Get : Get<GetRequestType, PortSet<SimulatedTimerState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<SimulatedTimerState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
}