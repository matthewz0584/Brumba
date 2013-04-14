using System;
using System.Linq;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    public partial class AckermanVehicleExEntity
    {
        public class Builder
        {
            private readonly AckermanVehicleExEntity _vehicle;
            private readonly AckermanVehicleProperties _props;

            public Builder(AckermanVehicleExEntity vehicle, AckermanVehicleProperties props)
            {
                _vehicle = vehicle;
                _props = props;
            }

            public void Build()
            {
                FillWheelsProperties();
                FillChassisPartsProperties();

                //v.State.Flags = EntitySimulationModifiers.Kinematic;

                _vehicle.ChassisParts = _props.ChassisPartsProperties.Select(BuildChassisPart).ToList();

                _props.WheelsProperties.Select(BuildWheel).ToList().ForEach(_vehicle.InsertEntity);
            }

            private void FillWheelsProperties()
            {
                foreach (var wp in _props.WheelsProperties)
                {
                    wp.Mass = _props.WheelMass;
                    wp.MaxSteeringAngle = _props.MaxSteeringAngle;
                    wp.PhysicalMesh = _props.WheelRadius == 0.05f ? "WheelShape50.obj" : _props.WheelRadius == 0.028f ? "WheelShape28.obj" : "need exception";
                    wp.VisualMesh = "CorobotWheel.obj";
                    wp.Radius = _props.WheelRadius == 0.05f ? 0.05f : _props.WheelRadius == 0.028f ? 0.028f : -10;
                    wp.Width = _props.WheelWidth;
                    wp.SuspensionRate = _props.SuspensionRate;
                }
            }

            private void FillChassisPartsProperties()
            {
                var i = 0;
                foreach (var chp in _props.ChassisPartsProperties)
                {
                    chp.LocalPose.Position = new Vector3(0, chp.Dimensions.Y / 2.0f + _props.Clearance, -_props.WheelBase / 2.0f - _props.WheelRadius + _props.ChassisPartsProperties.Take(i++).Aggregate(0f, (a, p) => a + p.Dimensions.Z) + chp.Dimensions.Z / 2);
                    chp.MassDensity.Mass = chp.MassDensity.Mass * _props.ChassisMass;
                    chp.Material = new MaterialProperties("ChassisMaterial", 0.0f, 0.5f, 0.5f);
                    chp.DiffuseColor = new Vector4(1, 0, 0, 0);
                }
            }

            private BoxShape BuildChassisPart(BoxShapeProperties partProps)
            {
                partProps.Name = VehiclePartName(partProps.Name);
                return new BoxShape(partProps);
            }

            private CompositeWheel BuildWheel(CompositeWheelProperties wheelProps)
            {
                wheelProps.Name = VehiclePartName(wheelProps.Name);
                return new CompositeWheel.Builder(_vehicle, wheelProps).Build();
            }

            private string VehiclePartName(string partName)
            {
                return String.Format("{0} {1}", _vehicle.State.Name, partName);
            }
        }
    }
}


