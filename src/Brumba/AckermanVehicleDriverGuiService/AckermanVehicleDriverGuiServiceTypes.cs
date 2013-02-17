using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.AckermanVehicleDriverGuiService
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2012/10/ackermanvehiclesdriverguiservice.html";
	}
	
	[DataContract]
	public class AckermanVehicleDriverGuiServiceState
	{
	}
	
	[ServicePort]
	public class AckermanVehicleDriverGuiServiceOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}
	
	public class Get : Get<GetRequestType, PortSet<AckermanVehicleDriverGuiServiceState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}
		
		public Get(GetRequestType body, PortSet<AckermanVehicleDriverGuiServiceState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

    public class MainWindowEvents : PortSet<OnPower, OnSteer, OnBreak>
    {
    }

    public class OnPower
    {
        public float Power { get; set; }
    }

    public class OnSteer
    {
        public float Direction { get; set; }
    }

    public class OnBreak
    {
    }
}