using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;

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

    [DataContract]
    public class InfraredRfRingProperties
    {
        [DataMember]
        public InfraredRfProperties InfraredRfProperties { get; set; }

        [DataMember]
        [Description("Rfs polar coordinates: X - angle, Y - radius. Zero angle along +Z axis")]
        public List<Vector2> RfPositionsPolar { get; set; }
    }
}