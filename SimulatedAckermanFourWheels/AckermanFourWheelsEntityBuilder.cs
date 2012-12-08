using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace Brumba.Simulation.SimulatedAckermanFourWheels
{
    public partial class AckermanFourWheelsEntity
    {
        public class Builder
        {
            private AckermanFourWheelsEntity _vehicle;
            private IEnumerable<CompositeWheelProperties> _wheelsProperties;
            private IEnumerable<BoxShapeProperties> _chassisPartsProperties;

            public static Builder Default
            {
                get
                {
                    float wheelRadius = 0.05f, distanceBetweenWheels = 0.17f, wheelWidth = 0.045f, wheelBase = 0.25f;
                    
                    return new Builder()
                    {
                        WheelBase = wheelBase,
                        DistanceBetweenWheels = distanceBetweenWheels,
                        WheelRadius = wheelRadius,
                        WheelWidth = wheelWidth,
                        WheelMass = 0.03f,
                        ChassisMass = 2f,
                        Clearance = 0.05f,
                        ChassisPartsProperties = new BoxShapeProperties[]
                        {
                            new BoxShapeProperties { Name = "ChassisBack", Dimensions = new Vector3(distanceBetweenWheels - wheelWidth, 0.04f, 2 * wheelRadius), MassDensity = { Mass = 0.1f } },
                            new BoxShapeProperties { Name = "ChassisMiddle", Dimensions = new Vector3(distanceBetweenWheels - wheelWidth, 0.10f, 0.13f), MassDensity = { Mass = 0.5f } },
                            new BoxShapeProperties { Name = "ChassisFront", Dimensions = new Vector3(distanceBetweenWheels - 2 * wheelWidth, 0.06f, 0.12f), MassDensity = { Mass = 0.4f } },
                        },
                        WheelsProperties = new CompositeWheelProperties[]
                        {
                            new CompositeWheelProperties { Name = "WheelFrontLeft", Position = new Vector3(distanceBetweenWheels / 2.0f, wheelRadius, wheelBase / 2.0f), Motorized = false, Steerable = true, Flipped = true},
                            new CompositeWheelProperties { Name = "WheelFrontRight", Position = new Vector3(-distanceBetweenWheels / 2.0f, wheelRadius, wheelBase / 2.0f), Motorized = false, Steerable = true, Flipped = false},
                            new CompositeWheelProperties { Name = "WheelRearLeft", Position = new Vector3(distanceBetweenWheels / 2.0f, wheelRadius, -wheelBase / 2.0f), Motorized = true, Steerable = false, Flipped = true},
                            new CompositeWheelProperties { Name = "WheelRearRight", Position = new Vector3(-distanceBetweenWheels / 2.0f, wheelRadius, -wheelBase / 2.0f), Motorized = true, Steerable = false, Flipped = false}
                        },
                        MaxVelocity = 4.16f, //15 km/h 
                        MaxSteerAngle = (float)Math.PI / 4
                    };
                }
            }

            private void FillWheelsProperties()
            {
                foreach (var wp in WheelsProperties)
                {
                    wp.Mass = WheelMass;
                    wp.MaxSteerAngle = MaxSteerAngle;
                    wp.PhysicalMesh = "WheelShape3.obj";
                    wp.VisualMesh = "CorobotWheel.obj";
                    wp.Radius = 0.05f;
                }
            }

            private void FillChassisPartsProperties()
            {
                var i = 0;
                foreach (var chp in ChassisPartsProperties)
                {
                    chp.LocalPose.Position = new Vector3(0, chp.Dimensions.Y / 2.0f + Clearance, -WheelBase / 2.0f - WheelRadius + ChassisPartsProperties.Take(i++).Aggregate(0f, (a, p) => a + p.Dimensions.Z) + chp.Dimensions.Z / 2);
                    chp.MassDensity.Mass = chp.MassDensity.Mass * ChassisMass;
                    chp.Material = new MaterialProperties("ChassisMaterial", 0.0f, 0.5f, 0.5f);
                    chp.DiffuseColor = new Vector4(1, 0, 0, 0);
                }
            }

            public void Build(AckermanFourWheelsEntity v)
            {
                _vehicle = v;

                _vehicle.MaxVelocity = MaxVelocity;
                _vehicle.MaxSteerAngle = MaxSteerAngle;

                //v.State.Flags = EntitySimulationModifiers.Kinematic;

                _vehicle.ChassisParts = ChassisPartsProperties.Select(BuildChassisPart).ToList();

                _vehicle.Wheels = WheelsProperties.Select(BuildWheel).ToList();
            }

            public float WheelRadius { get; set; }
            public float WheelWidth { get; set; }
            public float WheelMass { get; set; }
            public IEnumerable<CompositeWheelProperties> WheelsProperties 
            {
                get { return _wheelsProperties; }
                private set
                {
                    _wheelsProperties = value;
                    FillWheelsProperties();
                }
            }

            public float WheelBase { get; set; }
            public float DistanceBetweenWheels { get; set; }
            public float Clearance { get; set; }
            public float ChassisMass { get; set; }
            public IEnumerable<BoxShapeProperties> ChassisPartsProperties
            {
                get { return _chassisPartsProperties; }
                private set
                {
                    _chassisPartsProperties = value;
                    FillChassisPartsProperties();
                }
            }

            public float MaxVelocity { get; set; }
            public float MaxSteerAngle { get; set; }

            private BoxShape BuildChassisPart(BoxShapeProperties partProps)
            {
                partProps.Name = VehiclePartName(partProps.Name);
                return new BoxShape(partProps);
            }

            private CompositeWheel BuildWheel(CompositeWheelProperties wheelProps)
            {
                wheelProps.Name = VehiclePartName(wheelProps.Name);
                return new CompositeWheel(wheelProps);
            }

            private string VehiclePartName(string partName)
            {
                return String.Format("{0} {1}", _vehicle.State.Name, partName);
            }
        }
    }
}


