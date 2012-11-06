using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation;
using Microsoft.Ccr.Core;

namespace Brumba.Simulation.SimulatedAckermanFourWheels
{
    [DataContract]
    public class CompositeWheel
    {
        private WheelEntity Model { get; set; }
        private VisualEntity Body { get; set; }

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
        public float MaxSteerAngle { get; set; }

        [DataMember]
        public bool Motorized { get; set; }
        [DataMember]
        public bool Steerable { get; set; }
        [DataMember]
        public bool Flipped { get; set; }

        private VisualEntity Parent { get; set; }

        public CompositeWheel()
        {
        }

        public CompositeWheel(string name, Vector3 position, float mass, string physicalMesh)
        {
            Name = name;
            Position = position;
            Mass = mass;
            PhysicalMesh = physicalMesh;
            Radius = 0.05f; //GetRadiusFromMesh(physicalMesh) * 10f / 9f
        }

        public void Initialize(VisualEntity parent, GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            Parent = parent;

            //Build or rebuild model
            Model = BuildModel();
            Model.Parent = parent;
            Model.Initialize(device, physicsEngine);

            //Build body
            if (!Parent.Children.Any(ve => ve.State.Name == (Name + " body")))
            {
                Body = BuildBody();
                parent.InsertEntity(Body);
            }
            //Deserialize body
            else
            {
                Body = Parent.Children.Where(ve => ve.State.Name == (Name + " body")).Single();
            }
        }

        public void Render(VisualEntity.RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
            Model.Render(renderMode, transforms, currentCamera);
        }

        public void Update(FrameUpdate update)
        {
            Model.Update(update);
        }

        public void Dispose()
        {
            Model.Dispose();
        }

        public float AxleSpeed
        {
            get
            {
                return Model.Wheel.AxleSpeed;
            }
            set
            {
                Model.Wheel.AxleSpeed = value;
                ((PhysicsJoint)Body.ParentJoint).SetAngularDriveVelocity(new Vector3(0, value, 0));
            }
        }

        public float SteerAngle
        {
            get
            {
                return Model.Wheel.SteerAngle;
            }
            set
            {
                Model.Wheel.SteerAngle = value;
                ((PhysicsJoint)Body.ParentJoint).SetAngularDriveOrientation(Quaternion.FromAxisAngle(new AxisAngle(new Vector3(1, 0, 0), -value)));
            }
        }

        private WheelEntity BuildModel()
        {
            var wheel = new WheelEntity(new WheelShapeProperties(Name + " shape", Mass * 0.01f, Radius)
            {
                LocalPose = new Pose(Position),
                TireLateralForceFunction =
                {
                    AsymptoteValue = 0.001f,
                    StiffnessFactor = 1.0e8f
                },
                TireLongitudalForceFunction =
                {
                    AsymptoteValue = 0.001f,
                    StiffnessFactor = 1.0e8f
                }
            })
            {
                State =
                {
                    Name = Name + " model",
                    Assets = { Mesh = VisualMesh }
                },
            };

            if (Motorized)
                wheel.WheelShape.WheelState.Flags |= WheelShapeBehavior.OverrideAxleSpeed;
            if (Flipped)
                wheel.MeshRotation = new Vector3(0, 180, 0);
            return wheel;
        }

        private VisualEntity BuildBody()
        {
            var wheelBody = new SimplifiedConvexMeshEnvironmentEntity(Position, PhysicalMesh, null)
            {
                State =
                {
                    Name = Name + " body",
                    MassDensity = { Mass = Mass }
                },
                Material = new MaterialProperties("tire", 0.0f, 0.5f, 10.0f),
                Flags = VisualEntityProperties.DisableRendering
            };

            JointAngularProperties jointAngularProps = null;
            if (Motorized)
            {
                jointAngularProps = new JointAngularProperties
                {
                    Swing1Mode = JointDOFMode.Free,
                    SwingDrive = new JointDriveProperties(JointDriveMode.Velocity, new SpringProperties(), 1),
                };
            }

            if (Steerable)
            {
                jointAngularProps = new JointAngularProperties
                {
                    Swing1Mode = JointDOFMode.Free,
                    TwistMode = JointDOFMode.Limited,
                    TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(1000000, 10000, 0), 100000000),
                    UpperTwistLimit = new JointLimitProperties(MaxSteerAngle * 1.1f, 0, new SpringProperties()),
                    LowerTwistLimit = new JointLimitProperties(-MaxSteerAngle * 1.1f, 0, new SpringProperties()),
                };
            }

            //Magic!!! I don't know how simulator restores joint connectivity on deserialization.
            //Deserialization stops working if connectors are swapped in ctor call.
            var connector1 = new EntityJointConnector(wheelBody, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3());
            var connector2 = new EntityJointConnector(Parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), Position);

            wheelBody.ParentJoint = new Joint
            {
                State = new JointProperties(jointAngularProps, connector1, connector2)
                {
                    Name = Name + " joint",
                    EnableCollisions = false
                }
            };

            return wheelBody;
        }
    }
}
