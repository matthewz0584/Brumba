using Microsoft.Dss.Core.Attributes;

namespace Brumba.Simulation.SimulatedTurret
{
    [DataContract]
    public class TurretProperties
    {
        [DataMember]
        public float TwistPower { get; set; }

        [DataMember]
        public float BaseHeight { get; set; }
        [DataMember]
        public float BaseMass { get; set; }

        [DataMember]
        public float SegmentRadius { get; set; }
    }
}