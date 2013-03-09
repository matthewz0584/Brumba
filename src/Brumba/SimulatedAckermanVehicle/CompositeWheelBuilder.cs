using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    public partial class CompositeWheel
    {
        public class Builder
        {
            private readonly VisualEntity _parent;
            private readonly CompositeWheelProperties _props;

            public Builder(VisualEntity parent, CompositeWheelProperties props)
            {
                _props = props;
                _parent = parent;
            }

            public CompositeWheel Build()
            {
                var wheel = new CompositeWheel(_props);
                SetParams(wheel);
                wheel.ParentJoint = BuildJoint(_parent, wheel);
                wheel.ModelInner = BuildModel(!_props.Flipped);
                wheel.ModelOutter = BuildModel(_props.Flipped);
                return wheel;
            }

            private void SetParams(CompositeWheel wheel)
            {
                wheel.State.Name = _props.Name + " body";
                wheel.State.MassDensity.Mass = _props.Mass;
                wheel.State.Assets.Mesh = _props.PhysicalMesh;
                wheel.Material = new MaterialProperties("tire", 0.0f, 0, 0);
                wheel.Flags = VisualEntityProperties.DisableRendering;
            }

            private Joint BuildJoint(VisualEntity parent, CompositeWheel wheel)
            {
                var jointAngularProps = new JointAngularProperties();

                if (_props.Steerable)
                {
                    jointAngularProps.TwistMode = JointDOFMode.Limited;
                    jointAngularProps.TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(1000000, 10000, 0), 100000000);
                    jointAngularProps.UpperTwistLimit = new JointLimitProperties(_props.MaxSteerAngle * 1.1f, 0, new SpringProperties());
                    jointAngularProps.LowerTwistLimit = new JointLimitProperties(-_props.MaxSteerAngle * 1.1f, 0, new SpringProperties());
                }

                var jointLinearProps = new JointLinearProperties
                {
                    XMotionMode = JointDOFMode.Free,
                    XDrive = new JointDriveProperties(JointDriveMode.Position,
                                new SpringProperties(_props.SuspensionRate, _props.SuspensionRate / 10, 0), 10000)
                };

                var connector1 = new EntityJointConnector(wheel, new Vector3(1, 0, 0), new Vector3(0, 1, 0),
                                                          new Vector3()) { EntityName = wheel.State.Name };
                var connector2 = new EntityJointConnector(parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), _props.Position)
                {
                    EntityName = parent.State.Name
                };

                return new Joint
                {
                    State = new JointProperties(jointAngularProps, connector1, connector2)
                    {
                        Linear = jointLinearProps,
                        Name = _props.Name + " joint",
                        EnableCollisions = false
                    }
                };
            }

            private WheelEntity BuildModel(bool inner)
            {
                var wheel =
                    new WheelEntity(new WheelShapeProperties(_props.Name + (inner ? " model inner" : " model outter"), _props.Mass / 20, _props.Radius)
                    {
                        LocalPose = new Pose(new Vector3((inner ? 1 : -1) * _props.Width / 2, 0, 0)),
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
                            AsymptoteValue = 0.01f,
                            StiffnessFactor = 1.0e7f
                        }
                    })
                    {
                        State =
                        {
                            Name = _props.Name + (inner ? " model inner" : " model outter"),
                            Assets = { Mesh = _props.VisualMesh }
                        },
                    };
                if (_props.Motorized)
                    wheel.WheelShape.WheelState.Flags |= WheelShapeBehavior.OverrideAxleSpeed;
                if (_props.Flipped)
                    wheel.MeshRotation = new Vector3(0, 180, 0);
                return wheel;
            }
        }
    }
}
