using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

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

	/// <summary>
	/// ReferencePlatform2011 state
	/// </summary>
	[DataContract]
	public class State
	{
	}

	/// <summary>
	/// ReferencePlatform2011 main operations port
	/// </summary>
	[ServicePort]
	public class Operations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}

	/// <summary>
	/// ReferencePlatform2011 get operation
	/// </summary>
	public class Get : Get<GetRequestType, PortSet<State, Fault>>
	{
		/// <summary>
		/// Creates a new instance of Get
		/// </summary>
		public Get()
		{
		}

		/// <summary>
		/// Creates a new instance of Get
		/// </summary>
		/// <param name="body">The request message body</param>
		public Get(GetRequestType body)
			: base(body)
		{
		}

		/// <summary>
		/// Creates a new instance of Get
		/// </summary>
		/// <param name="body">The request message body</param>
		/// <param name="responsePort">The response port for the request</param>
		public Get(GetRequestType body, PortSet<State, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}
}