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
    public class CompositeWheelProperties
    {
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
    }

    [DataContract]
    public class CompositeWheel
    {
        [DataMember]
        public CompositeWheelProperties Props { get; set; }

        private WheelEntity Model { get; set; }
        private VisualEntity Body { get; set; }

        private VisualEntity Parent { get; set; }

        public CompositeWheel()
        {
        }

        public CompositeWheel(CompositeWheelProperties props)
        {
            Props = props;
        }

        public void Initialize(VisualEntity parent, GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            Parent = parent;

            //Build or rebuild model
            Model = BuildModel();
            Model.Parent = parent;
            Model.Initialize(device, physicsEngine);

            //Build body
            if (!Parent.Children.Any(ve => ve.State.Name == (Props.Name + " body")))
            {
                Body = BuildBody();
                parent.InsertEntity(Body);
            }
            //Deserialize body
            else
            {
                Body = Parent.Children.Where(ve => ve.State.Name == (Props.Name + " body")).Single();
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
            var wheel = new WheelEntity(new WheelShapeProperties(Props.Name + " shape", Props.Mass * 0.01f, Props.Radius)
            {
                LocalPose = new Pose(Props.Position),
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
                    Name = Props.Name + " model",
                    Assets = { Mesh = Props.VisualMesh }
                },
            };

            if (Props.Motorized)
                wheel.WheelShape.WheelState.Flags |= WheelShapeBehavior.OverrideAxleSpeed;
            if (Props.Flipped)
                wheel.MeshRotation = new Vector3(0, 180, 0);
            return wheel;
        }

        private VisualEntity BuildBody()
        {
            var wheelBody = new SimplifiedConvexMeshEnvironmentEntity(Props.Position, Props.PhysicalMesh, null)
            {
                State =
                {
                    Name = Props.Name + " body",
                    MassDensity = { Mass = Props.Mass }
                },
                Material = new MaterialProperties("tire", 0.0f, 0.5f, 10.0f),
                Flags = VisualEntityProperties.DisableRendering
            };

            JointAngularProperties jointAngularProps = new JointAngularProperties { Swing1Mode = JointDOFMode.Free };

            if (Props.Motorized)
                jointAngularProps.SwingDrive = new JointDriveProperties(JointDriveMode.Velocity, new SpringProperties(), 1);

            if (Props.Steerable)
            {
                jointAngularProps.TwistMode = JointDOFMode.Limited;
                jointAngularProps.TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(1000000, 10000, 0), 100000000);
                jointAngularProps.UpperTwistLimit = new JointLimitProperties(Props.MaxSteerAngle * 1.1f, 0, new SpringProperties());
                jointAngularProps.LowerTwistLimit = new JointLimitProperties(-Props.MaxSteerAngle * 1.1f, 0, new SpringProperties());
            }

            var connector1 = new EntityJointConnector(wheelBody, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3()) 
                                { EntityName = wheelBody.State.Name };
            var connector2 = new EntityJointConnector(Parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), Props.Position) 
                                { EntityName = Parent.State.Name };

            wheelBody.ParentJoint = new Joint
            {
                State = new JointProperties(jointAngularProps, connector1, connector2)
                {
                    Name = Props.Name + " joint",
                    EnableCollisions = false
                }
            };

            return wheelBody;
        }
    }
}
