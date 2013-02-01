using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;

namespace Brumba.Simulation.AckermanFourWheelsDriverGuiService
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2012/10/ackermanfourwheelsdriverguiservice.html";
	}
	
	[DataContract]
	public class AckermanFourWheelsDriverGuiServiceState
	{
	}
	
	[ServicePort]
	public class AckermanFourWheelsDriverGuiServiceOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}
	
	public class Get : Get<GetRequestType, PortSet<AckermanFourWheelsDriverGuiServiceState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}
		
		public Get(GetRequestType body, PortSet<AckermanFourWheelsDriverGuiServiceState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

    public class MainWindowEvents : PortSet<OnPower, OnSteer, OnBreak>
    {
    }

    public class OnPower
    {
        public float Direction { get; set; }
    }

    public class OnSteer
    {
        public float Direction { get; set; }
    }

    public class OnBreak
    {
    }
}


