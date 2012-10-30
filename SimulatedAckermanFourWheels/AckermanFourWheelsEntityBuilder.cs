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
            private static readonly MaterialProperties ChassisMaterial = new MaterialProperties("ChassisMaterial", 0.0f, 0.5f, 0.5f);

            private AckermanFourWheelsEntity _vehicle;

            public static Builder Default
            {
                get
                {
                    return new Builder()
                    {
                        WheelBase = 0.25f,
                        DistanceBetweenWheels = 0.17f,
                        WheelRadius = 0.05f,
                        WheelWidth = 0.045f,
                        WheelMass = 0.05f,
                        ChassisMass = 2f,
                        Clearance = 0.05f,
                        ChassisHeights = new float[] { 0.04f, 0.10f, 0.06f },
                        MaxVelocity = 4.16f, //15 km/h 
                        MaxSteerAngle = (float)Math.PI / 4
                    };
                }
            }

            public void Build(AckermanFourWheelsEntity v)
            {
                _vehicle = v;

                _vehicle.MaxVelocity = MaxVelocity;
                _vehicle.MaxSteerAngle = MaxSteerAngle;

                //v.State.Flags = EntitySimulationModifiers.Kinematic;

                _vehicle.ChassisParts = new List<BoxShape>();
                _vehicle.ChassisParts.Add(BuildChassisPart(0, "ChassisBack", 0.2f));
                _vehicle.ChassisParts.Add(BuildChassisPart(1, "ChassisMiddle", 0.5f));
                _vehicle.ChassisParts.Add(BuildChassisPart(2, "ChassisFront", 0.3f));

                _vehicle.WheelFl = BuildWheel(WheelFullName("WheelFrontLeft"), new Vector3(DistanceBetweenWheels / 2.0f, WheelRadius, WheelBase / 2.0f), false, true, true);
                _vehicle.WheelFr = BuildWheel(WheelFullName("WheelFrontRight"), new Vector3(-DistanceBetweenWheels / 2.0f, WheelRadius, WheelBase / 2.0f), false, true, false);
                _vehicle.WheelRl = BuildWheel(WheelFullName("WheelRearLeft"), new Vector3(DistanceBetweenWheels / 2.0f, WheelRadius, -WheelBase / 2.0f), true, false, true);
                _vehicle.WheelRr = BuildWheel(WheelFullName("WheelRearRight"), new Vector3(-DistanceBetweenWheels / 2.0f, WheelRadius, -WheelBase / 2.0f), true, false, false);
            }

            public float WheelBase { get; set; }
            public float DistanceBetweenWheels { get; set; }
            public float WheelRadius { get; set; }
            public float WheelWidth { get; set; }

            public float[] ChassisHeights { get; set; }
            public float Clearance { get; set; }

            public float ChassisMass { get; set; }
            public float WheelMass { get; set; }

            public float MaxVelocity { get; set; }
            public float MaxSteerAngle { get; set; }

            private BoxShape BuildChassisPart(int number, string name, float massFactor)
            {
                //Vehicle origin is the middle of wheel base
                var position = new Vector3(0, ChassisHeights[number] / 2.0f + Clearance, -WheelBase / 2.0f - WheelRadius + ChassisLengths.Take(number).Aggregate(0f, (a, l) => a + l) + ChassisLengths[number] / 2);
                var dimensions = new Vector3(ChassisWidth[number], ChassisHeights[number], ChassisLengths[number]);
                return new BoxShape(new BoxShapeProperties(name, ChassisMass * massFactor, new Pose(position), dimensions)
                {
                    Material = ChassisMaterial,
                    DiffuseColor = new Vector4(1, 0, 0, 0)
                });
            }

            private CompositeWheel BuildWheel(string name, Vector3 position, bool motorized, bool steerable, bool flipped)
            {
                return new CompositeWheel(name, position, WheelMass, "WheelShape3.obj")
                { 
                    Motorized = motorized,
                    Steerable = steerable,
                    Flipped = flipped,
                    VisualMesh = "CorobotWheel.obj",
                    MaxSteerAngle = MaxSteerAngle
                };
            }

            private string WheelFullName(string wheelName)
            {
                return String.Format("{0} {1}", _vehicle.State.Name, wheelName);
            }

            private float[] ChassisWidth
            {
                get { return new float[] { DistanceBetweenWheels - WheelWidth, DistanceBetweenWheels - WheelWidth, DistanceBetweenWheels - 2 * WheelWidth }; }
            }

            private float[] ChassisLengths
            {
                get { return new float[] { 2 * WheelRadius, 0.13f, 0.12f }; }
            }
        }
    }
}


