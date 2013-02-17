using System.Collections.Generic;
using Brumba.Simulation.SimulatedAckermanVehicleEx;
using Microsoft.Robotics.Simulation.Physics;

namespace Brumba.Simulation
{
    public class AckermanVehicleProperties
    {
        public float WheelRadius { get; set; }
        public float WheelWidth { get; set; }
        public float WheelMass { get; set; }
        public float SuspensionRate { get; set; }
        public IEnumerable<CompositeWheelProperties> WheelsProperties { get; set; }

        public float WheelBase { get; set; }
        public float WheelsSpacing { get; set; }
        public float Clearance { get; set; }
        public float ChassisMass { get; set; }
        public IEnumerable<BoxShapeProperties> ChassisPartsProperties { get; set; }

        public float MaxVelocity { get; set; }
        public float MaxSteerAngle { get; set; }
    }
}