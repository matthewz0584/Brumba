using System.ComponentModel;
using Brumba.DwaNavigator;
using Brumba.GenericLocalizer;
using Brumba.GenericVelocimeter;
using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedLocalizer
{
	public sealed class Contract
	{
		[DataMember]
		public const string Identifier = "http://brumba.ru/contracts/2014/11/simulatedlocalizer.html";
	}
	
	[DataContract]
	public class SimulatedLocalizerState : IConnectable
	{
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool IsConnected { get; set; }

        [DataMember]
        public GenericLocalizerState Localizer { get; set; }

        [DataMember]
        public GenericVelocimeterState Velocimeter { get; set; }

        [DataMember]
        public Velocity MaxVelocity { get; set; }
	}
	
	[ServicePort]
    public class SimulatedLocalizerOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}

    public class Get : Get<GetRequestType, PortSet<SimulatedLocalizerState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}

        public Get(GetRequestType body, PortSet<SimulatedLocalizerState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}