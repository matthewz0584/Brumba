using System.ComponentModel;
using Brumba.AckermanVehicle;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2013/02/simulatedackermanvehicle.html";
	}

    [DataContract]
    public class SimulatedAckermanVehicleState : AckermanVehicleState
    {
        [DataMember]
        [Description("If there is any simulation entity under control of this service")]
        public bool Connected { get; set; }
    }

    [ServicePort]
    public class SimulatedAckermanVehicleOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    public class Get : Get<GetRequestType, PortSet<SimulatedAckermanVehicleState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<SimulatedAckermanVehicleState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
}