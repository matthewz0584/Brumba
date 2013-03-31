using System;
using System.Collections.Generic;
using Brumba.Simulation.SimulatedAckermanVehicle;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Physics;

namespace Brumba.Simulation
{
    public class AckermanVehicles
    {
        public static AckermanVehicleProperties HardRearDriven
        {
            get
            {
                float wheelRadius = 0.05f, wheelsSpacing = 0.17f, wheelWidth = 0.045f, wheelBase = 0.25f;

                return new AckermanVehicleProperties
                    {
                        WheelBase = wheelBase,
                        WheelsSpacing = wheelsSpacing,
                        WheelRadius = wheelRadius,
                        WheelWidth = wheelWidth,
                        WheelMass = 0.03f,
                        ChassisMass = 2f,
                        Clearance = 0.05f,
                        SuspensionRate = 75000,
                        MaxVelocity = 4.16f, //15 km/h 
                        MaxSteeringAngle = (float)Math.PI / 4,
                        ChassisPartsProperties = new List<BoxShapeProperties>
                            {
                                new BoxShapeProperties { Name = "ChassisBack", Dimensions = new Vector3(wheelsSpacing - wheelWidth, 0.04f, 2 * wheelRadius), MassDensity = { Mass = 0.1f } },
                                new BoxShapeProperties { Name = "ChassisMiddle", Dimensions = new Vector3(wheelsSpacing - wheelWidth, 0.10f, 0.13f), MassDensity = { Mass = 0.5f } },
                                new BoxShapeProperties { Name = "ChassisFront", Dimensions = new Vector3(wheelsSpacing - 2 * wheelWidth, 0.06f, 0.1f), MassDensity = { Mass = 0.4f } },
                            },
                        WheelsProperties = new List<CompositeWheelProperties>
                            {
                                new CompositeWheelProperties { Name = "WheelFrontLeft", Position = new Vector3(wheelsSpacing / 2.0f, wheelRadius, wheelBase / 2.0f), Motorized = false, Steerable = true, Flipped = true},
                                new CompositeWheelProperties { Name = "WheelFrontRight", Position = new Vector3(-wheelsSpacing / 2.0f, wheelRadius, wheelBase / 2.0f), Motorized = false, Steerable = true, Flipped = false},
                                new CompositeWheelProperties { Name = "WheelRearLeft", Position = new Vector3(wheelsSpacing / 2.0f, wheelRadius, -wheelBase / 2.0f), Motorized = true, Steerable = false, Flipped = true},
                                new CompositeWheelProperties { Name = "WheelRearRight", Position = new Vector3(-wheelsSpacing / 2.0f, wheelRadius, -wheelBase / 2.0f), Motorized = true, Steerable = false, Flipped = false}
                            },
                    };
            }
        }

        public static AckermanVehicleProperties SuspendedRearDriven
        {
            get
            {
                var props = HardRearDriven;
                props.SuspensionRate = 750;
                return props;
            }
        }

        public static AckermanVehicleProperties Hard4x4
        {
            get
            {
                var props = HardRearDriven;
                foreach (var wp in props.WheelsProperties)
                    wp.Motorized = true;
                return props;
            }
        }

        public static AckermanVehicleProperties Suspended4x4
        {
            get
            {
                var props = Hard4x4;
                props.SuspensionRate = 750;
                return props;
            }
        }

        public static AckermanVehicleProperties Simplistic
        {
            get
            {
                float wheelRadius = 0.028f, wheelsSpacing = 0.135f, wheelWidth = 0.026f, wheelBase = 0.135f;

                return new AckermanVehicleProperties
                {
                    WheelBase = wheelBase,
                    WheelsSpacing = wheelsSpacing,
                    WheelRadius = wheelRadius,
                    WheelWidth = wheelWidth,
                    WheelMass = 0.03f,
                    ChassisMass = 1.05f,
                    Clearance = 0.028f,
                    SuspensionRate = 75000,
                    MaxVelocity = 1f, //?
                    MaxSteeringAngle = (float)Math.PI / 5,
                    ChassisPartsProperties = new List<BoxShapeProperties>
                            {
                                new BoxShapeProperties { Name = "Chassis", Dimensions = new Vector3(wheelsSpacing - wheelWidth, 0.085f, 0.165f), MassDensity = { Mass = 1.1f } },
                            },
                    WheelsProperties = new List<CompositeWheelProperties>
                            {
                                new CompositeWheelProperties { Name = "WheelFrontLeft", Position = new Vector3(wheelsSpacing / 2.0f, wheelRadius, wheelBase / 2.0f), Motorized = false, Steerable = true, Flipped = true},
                                new CompositeWheelProperties { Name = "WheelFrontRight", Position = new Vector3(-wheelsSpacing / 2.0f, wheelRadius, wheelBase / 2.0f), Motorized = false, Steerable = true, Flipped = false},
                                new CompositeWheelProperties { Name = "WheelRearLeft", Position = new Vector3(wheelsSpacing / 2.0f, wheelRadius, -wheelBase / 2.0f), Motorized = true, Steerable = false, Flipped = true},
                                new CompositeWheelProperties { Name = "WheelRearRight", Position = new Vector3(-wheelsSpacing / 2.0f, wheelRadius, -wheelBase / 2.0f), Motorized = true, Steerable = false, Flipped = false}
                            },
                };
            }
        }
    }
}