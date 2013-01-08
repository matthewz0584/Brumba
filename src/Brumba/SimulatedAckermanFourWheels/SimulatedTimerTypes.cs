using Microsoft.Dss.Core.Attributes;
using System.ComponentModel;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Ccr.Core;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedTimer
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://schemas.tempuri.org/2012/10/simulatedtimer.html";
    }

    [DataContract]
    public class SimulatedTimerState
    {
        [DataMember]
        [Description("Elapsed simulation time since entity initialization")]
        public double ElapsedTime { get; set; }

        [DataMember]
        [Description("Simulation time at the moment of entity initialization")]
        public double StartTime { get; set; }
    }

    [ServicePort]
    public class SimulatedTimerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, FirstInterleaveShutdown>
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

    [DataContract]
    public class FirstInterleaveShutdownRequest
    {
    }

    public class FirstInterleaveShutdown : Update<FirstInterleaveShutdownRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }
}