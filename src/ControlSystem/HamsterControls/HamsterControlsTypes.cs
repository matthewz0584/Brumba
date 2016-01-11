using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.HamsterControls
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2012/10/hamstercontrols.html";
	}
	
	[DataContract]
	public class HamsterControlsState
	{
	}
	
	[ServicePort]
	public class HamsterControlsOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}
	
	public class Get : Get<GetRequestType, PortSet<HamsterControlsState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}
		
		public Get(GetRequestType body, PortSet<HamsterControlsState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

    public class MainWindowEvents : PortSet<PowerRequest, SteerRequest, BreakRequest, TurretBaseAngleRequest>
    {
    }

    public class PowerRequest
    {
        public float Value { get; set; }
    }

    public class SteerRequest
    {
        public float Value { get; set; }
    }

    public class BreakRequest
    {
    }

    public class TurretBaseAngleRequest
    {
        public float Value { get; set; }
    }
}