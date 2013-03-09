using System.ComponentModel;
using Brumba.AckermanVehicle;
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
    public class SimulatedAckermanVehicleExState : AckermanVehicleState
	{
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool Connected { get; set; }
	}
	
	[ServicePort]
    public class SimulatedAckermanVehicleExOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
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
}