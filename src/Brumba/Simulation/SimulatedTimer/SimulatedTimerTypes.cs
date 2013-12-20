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
        public const string Identifier = "http://brumba.ru/contracts/2012/10/simulatedtimer.html";
    }

    [DataContract]
    public class SimulatedTimerState : ISimulationEntityServiceState
    {
        [DataMember]
        [Description("Elapsed simulation time since entity initialization")]
        public double ElapsedTime { get; set; }

        [DataMember]
        [Description("Simulation time at the moment of entity initialization")]
        public double StartTime { get; set; }

		[DataMember]
		[Description("If there is any simulation entity under control of this service")]
		public bool IsConnected { get; set; }
    }

    [ServicePort]
    public class SimulatedTimerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, Subscribe, Update>
    {
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

    public class Update : Update<SimulatedTimerState, PortSet<DefaultUpdateResponseType, Fault>>
    {
        public Update()
        {
        }

        public Update(SimulatedTimerState state)
        {
            Body = state;
        }
    }
}