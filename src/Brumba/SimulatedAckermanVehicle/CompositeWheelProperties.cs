using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    [DataContract]
    public class CompositeWheelProperties
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string PhysicalMesh { get; set; }
        [DataMember]
        public string VisualMesh { get; set; }

        [DataMember]
        public Vector3 Position { get; set; }
        [DataMember]
        public float Mass { get; set; }
        [DataMember]
        public float Radius { get; set; }
        [DataMember]
        public float Width { get; set; }
        [DataMember]
        public float MaxSteeringAngle { get; set; }
        [DataMember]
        public float SuspensionRate { get; set; }

        [DataMember]
        public bool Motorized { get; set; }
        [DataMember]
        public bool Steerable { get; set; }
        [DataMember]
        public bool Flipped { get; set; }
    }
}