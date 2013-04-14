using System.Collections.Generic;
using Brumba.Simulation.SimulatedAckermanVehicle;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation.Physics;

namespace Brumba.Simulation
{
    [DataContract]
    public class AckermanVehicleProperties
    {
        [DataMember]
        public float WheelRadius { get; set; }
        [DataMember]
        public float WheelWidth { get; set; }
        [DataMember]
        public float WheelMass { get; set; }
        [DataMember]
        public float SuspensionRate { get; set; }
        [DataMember]
        public List<CompositeWheelProperties> WheelsProperties { get; set; }

        [DataMember]
        public float WheelBase { get; set; }
        [DataMember]
        public float WheelsSpacing { get; set; }
        [DataMember]
        public float Clearance { get; set; }
        [DataMember]
        public float ChassisMass { get; set; }
        [DataMember]
        public List<BoxShapeProperties> ChassisPartsProperties { get; set; }

        [DataMember]
        public float MaxVelocity { get; set; }
        [DataMember]
        public float MaxSteeringAngle { get; set; }
    }
}