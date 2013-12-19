using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;
using SickLrf = Microsoft.Robotics.Services.Sensors.SickLRF;

namespace Brumba.Simulation.SimulatedLrf
{
	/// <summary>
	/// LaserRangeFinder Contract
	/// </summary>
	public static class Contract
	{
		/// <summary>
		/// LaserRangeFinder unique contract identifier 
		/// </summary>
		public const string Identifier = "http://brumba.ru/contracts/2013/11/simulatedlrf.html";
	}

	[DataContract]
	public class SimulatedLrfState
	{
		[DataMember]
		public SickLrf.State SickLrfState { get; set; }

		[DataMember]
		[Description("If there is any simulation entity under control of this service")]
		public bool Connected { get; set; }
	}

	[ServicePort]
	public class SimulatedLrfOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}

	public class Get : Get<GetRequestType, PortSet<SimulatedLrfState, Fault>>
	{
		public Get()
		{
		}

		public Get(GetRequestType body)
			: base(body)
		{
		}

		public Get(GetRequestType body, PortSet<SimulatedLrfState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}