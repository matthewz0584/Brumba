using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;

namespace Brumba.Simulation.SimulatedAckermanFourWheels
{
    public partial class AckermanFourWheelsEntity
    {
        public class Builder
        {
            private AckermanFourWheelsEntity _vehicle;

        	public static Builder HardRearDriven
            {
                get
                {
                    float wheelRadius = 0.05f, distanceBetweenWheels = 0.17f, wheelWidth = 0.045f, wheelBase = 0.25f;
                    
                    return new Builder
                    {
                        WheelBase = wheelBase,
                        DistanceBetweenWheels = distanceBetweenWheels,
                        WheelRadius = wheelRadius,
                        WheelWidth = wheelWidth,
                        WheelMass = 0.03f,
                        ChassisMass = 2f,
                        Clearance = 0.05f,
						SuspensionRate = 75000,
                        MaxVelocity = 4.16f, //15 km/h 
                        MaxSteerAngle = (float)Math.PI / 4,
                        ChassisPartsProperties = new []
                        {
                            new BoxShapeProperties { Name = "ChassisBack", Dimensions = new Vector3(distanceBetweenWheels - wheelWidth, 0.04f, 2 * wheelRadius), MassDensity = { Mass = 0.1f } },
                            new BoxShapeProperties { Name = "ChassisMiddle", Dimensions = new Vector3(distanceBetweenWheels - wheelWidth, 0.10f, 0.13f), MassDensity = { Mass = 0.5f } },
                            new BoxShapeProperties { Name = "ChassisFront", Dimensions = new Vector3(distanceBetweenWheels - 2 * wheelWidth, 0.06f, 0.1f), MassDensity = { Mass = 0.4f } },
                        },
                        WheelsProperties = new []
                        {
                            new CompositeWheelProperties { Name = "WheelFrontLeft", Position = new Vector3(distanceBetweenWheels / 2.0f, wheelRadius, wheelBase / 2.0f), Motorized = false, Steerable = true, Flipped = true},
                            new CompositeWheelProperties { Name = "WheelFrontRight", Position = new Vector3(-distanceBetweenWheels / 2.0f, wheelRadius, wheelBase / 2.0f), Motorized = false, Steerable = true, Flipped = false},
                            new CompositeWheelProperties { Name = "WheelRearLeft", Position = new Vector3(distanceBetweenWheels / 2.0f, wheelRadius, -wheelBase / 2.0f), Motorized = true, Steerable = false, Flipped = true},
                            new CompositeWheelProperties { Name = "WheelRearRight", Position = new Vector3(-distanceBetweenWheels / 2.0f, wheelRadius, -wheelBase / 2.0f), Motorized = true, Steerable = false, Flipped = false}
                        },
                    };
                }
            }

			public static Builder SuspendedRearDriven
			{
				get
				{
					var builder = HardRearDriven;
					builder.SuspensionRate = 750;
					return builder;
				}
			}

        	public static Builder Hard4x4
            {
                get
                {
					var builder = HardRearDriven;
                    foreach (var wp in builder.WheelsProperties)
                        wp.Motorized = true;
                    return builder;
                }
            }

			public static Builder Suspended4x4
			{
				get
				{
					var builder = Hard4x4;
					builder.SuspensionRate = 750;
					return builder;
				}
			}

            public void Build(AckermanFourWheelsEntity v)
            {
            	FillWheelsProperties();
            	FillChassisPartsProperties();

                _vehicle = v;

                _vehicle.MaxVelocity = MaxVelocity;
                _vehicle.MaxSteerAngle = MaxSteerAngle;

                //v.State.Flags = EntitySimulationModifiers.Kinematic;

                _vehicle.ChassisParts = ChassisPartsProperties.Select(BuildChassisPart).ToList();

				WheelsProperties.Select(BuildWheel).ToList().ForEach(_vehicle.InsertEntity);
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
					wp.SuspensionRate = SuspensionRate;
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

            public float WheelRadius { get; set; }
            public float WheelWidth { get; set; }
            public float WheelMass { get; set; }
			public float SuspensionRate { get; set; }
        	public IEnumerable<CompositeWheelProperties> WheelsProperties { get; private set; }

        	public float WheelBase { get; set; }
            public float DistanceBetweenWheels { get; set; }
            public float Clearance { get; set; }
            public float ChassisMass { get; set; }
        	public IEnumerable<BoxShapeProperties> ChassisPartsProperties { get; private set; }

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
            	return wheelProps.Build(_vehicle);
            }

            private string VehiclePartName(string partName)
            {
                return String.Format("{0} {1}", _vehicle.State.Name, partName);
            }
        }
    }
}


