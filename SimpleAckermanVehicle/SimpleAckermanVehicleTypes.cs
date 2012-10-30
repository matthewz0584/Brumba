using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;

namespace Brumba.Simulation
{
	public sealed class Contract
	{
		[DataMember]
		public const string Identifier = "http://schemas.tempuri.org/2012/10/simpleackermanvehicle.html";
	}
	
	[DataContract]
	public class SimpleAckermanVehicleState
	{
	}
	
	[ServicePort]
	public class SimpleAckermanVehicleOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}
	
	public class Get : Get<GetRequestType, PortSet<SimpleAckermanVehicleState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}
		
		public Get(GetRequestType body, PortSet<SimpleAckermanVehicleState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}


