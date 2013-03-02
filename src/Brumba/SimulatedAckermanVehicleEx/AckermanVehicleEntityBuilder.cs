using System;
using System.Linq;
using Brumba.Simulation.SimulatedAckermanVehicleEx;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    public partial class AckermanVehicleEntity
    {
        public class Builder
        {
            private readonly AckermanVehicleEntity _vehicle;
            private readonly AckermanVehicleProperties _props;

            public Builder(AckermanVehicleEntity vehicle, AckermanVehicleProperties props)
            {
                _vehicle = vehicle;
                _props = props;
            }

            public void Build()
            {
                FillWheelsProperties();
                FillChassisPartsProperties();

                //v.State.Flags = EntitySimulationModifiers.Kinematic;

                _vehicle.Chassis = BuildChassisPart(_props.ChassisPartsProperties.First());

                _vehicle.Wheels = _props.WheelsProperties.Select(BuildWheel).ToList();
            }

            private void FillWheelsProperties()
            {
                foreach (var wp in _props.WheelsProperties)
                {
                    wp.Mass = _props.WheelMass;
                    wp.MaxSteerAngle = _props.MaxSteerAngle;
                    wp.PhysicalMesh = null;
                    wp.VisualMesh = "CorobotWheel.obj";
                    wp.Radius = _props.WheelRadius;
                    wp.Width = _props.WheelWidth;
                    wp.SuspensionRate = 0;
                }
            }

            private void FillChassisPartsProperties()
            {
                var chp = _props.ChassisPartsProperties.First();
                chp.LocalPose.Position = new Vector3(0, chp.Dimensions.Y / 2.0f + _props.Clearance, -_props.WheelBase / 2.0f - _props.WheelRadius + chp.Dimensions.Z / 2);
                chp.MassDensity.Mass = _props.ChassisMass;
                chp.Material = new MaterialProperties("ChassisMaterial", 0.0f, 0.5f, 0.5f);
                chp.DiffuseColor = new Vector4(1, 0, 0, 0);
            }

            private BoxShape BuildChassisPart(BoxShapeProperties partProps)
            {
                partProps.Name = VehiclePartName(partProps.Name);
                return new BoxShape(partProps);
            }

            private WheelEntity BuildWheel(CompositeWheelProperties wp)
            {
                wp.Name = VehiclePartName(wp.Name);

                var wheel = new WheelEntity(new WheelShapeProperties(wp.Name, wp.Mass, wp.Radius)
                {
                    LocalPose = new Pose(wp.Position),
                    TireLongitudalForceFunction =
                    {
                        ExtremumSlip = 1.0f,
                        ExtremumValue = 0.02f,
                        AsymptoteSlip = 2.0f,
                        AsymptoteValue = 0.01f,
                        StiffnessFactor = 1.0e8f
                    },
                    TireLateralForceFunction =
                    {
                        ExtremumSlip = 1.0f,
                        ExtremumValue = 0.02f,
                        AsymptoteSlip = 2.0f,
                        AsymptoteValue = 0.02f,
                        StiffnessFactor = 1.0e7f
                    }
                })
                {
                    State =
                    {
                        Name = wp.Name,
                        Assets = { Mesh = wp.VisualMesh }
                    },
                    MeshScale = new Vector3(0.6f, 0.6f, 0.6f)
                };
                if (wp.Motorized)
                    wheel.WheelShape.WheelState.Flags |= WheelShapeBehavior.OverrideAxleSpeed;
                if (wp.Flipped)
                    wheel.MeshRotation = new Vector3(0, 180, 0);
                return wheel;
            }

            private string VehiclePartName(string partName)
            {
                return String.Format("{0} {1}", _vehicle.State.Name, partName);
            }
        }
    }
}
