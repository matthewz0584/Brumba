using Microsoft.Dss.Core.Attributes;

namespace Brumba.Simulation.SimulatedInfraredRfRing
{
    [DataContract]
    public class InfraredRfProperties
    {
        [DataMember]
        public float MaximumRange { get; set; }

        [DataMember]
        public float Samples { get; set; }

        [DataMember]
        public float DispersionConeAngle { get; set; }

        [DataMember]
        public float ScanInterval { get; set; }        
    }
}