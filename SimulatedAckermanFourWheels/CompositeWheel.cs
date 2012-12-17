using System;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Dss.Core.Attributes;

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
    public class CompositeWheel : SimplifiedConvexMeshEnvironmentEntity
    {
        [DataMember]
        public CompositeWheelProperties Props { get; set; }

		[DataMember]
		public WheelEntity Model { get; set; }

        public CompositeWheel()
        {
        }

        public CompositeWheel(CompositeWheelProperties props)
            : base(props.Position, props.PhysicalMesh, null)
        {
            Props = props;

            State.Name = Props.Name + " body";
            State.MassDensity.Mass = Props.Mass * 0.5f;
            State.Assets.Mesh = Props.PhysicalMesh;
            Material = new MaterialProperties("tire", 0.0f, 0, 0);
            Flags = VisualEntityProperties.DisableRendering;
        }

        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);
            PhysicsEntity.SolverIterationCount = 64;

            if (Model == null)
				Model = BuildModel();

            Model.Parent = this;
            Model.Initialize(device, physicsEngine);
        }

        public override void Update(FrameUpdate update)
        {
            Model.Update(update);
            base.Update(update);

            //var qq = Quaternion.ToAxisAngle(State.Pose.Orientation);
            //if (float.IsNaN(qq.Angle))
            //    return;
            //var angle = Math.Sign(qq.Axis.X) * qq.Angle + (float)update.ElapsedTime * Model.Wheel.AxleSpeed;
            //Parent.PhysicsEntity.SetShapeLocalPose(State.PhysicsPrimitives[1], new Pose(new Vector3(), Quaternion.FromAxisAngle(1, 0, 0, angle)));
        }

        public override void Render(RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
            Model.Render(renderMode, transforms, currentCamera);
            base.Render(renderMode, transforms, currentCamera);
        }

        public override void Dispose()
        {
            Model.Dispose();
            base.Dispose();
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
            }
        }

        public float SteerAngle
        {
            get
            {
                var localOrientation = Quaternion.ToAxisAngle(State.Pose.Orientation * Quaternion.Inverse(Parent.State.Pose.Orientation));
                return Math.Sign(localOrientation.Axis.Y) * localOrientation.Angle;
            }
            set
            {
                ((PhysicsJoint)ParentJoint).SetAngularDriveOrientation(Quaternion.FromAxisAngle(new AxisAngle(new Vector3(1, 0, 0), -value)));
            }
        }

        public void Build()
        {
            var jointAngularProps = new JointAngularProperties();

            if (Props.Steerable)
            {
                jointAngularProps.TwistMode = JointDOFMode.Limited;
                jointAngularProps.TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(1000000, 10000, 0), 100000000);
                jointAngularProps.UpperTwistLimit = new JointLimitProperties(Props.MaxSteerAngle * 1.1f, 0, new SpringProperties());
                jointAngularProps.LowerTwistLimit = new JointLimitProperties(-Props.MaxSteerAngle * 1.1f, 0, new SpringProperties());
            }

            var jointLinearProps = new JointLinearProperties
            {
                XMotionMode = JointDOFMode.Free,
                XDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(500, 10, 0), 10000)
            };

            var connector1 = new EntityJointConnector(this, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3()) { EntityName = State.Name };
            var connector2 = new EntityJointConnector(Parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), Props.Position) { EntityName = Parent.State.Name };

            ParentJoint = new Joint
            {
                State = new JointProperties(jointAngularProps, connector1, connector2)
                {
                    Linear = jointLinearProps,
                    Name = Props.Name + " joint",
                    EnableCollisions = false
                }
            };
        }

        private WheelEntity BuildModel()
        {
            var wheel = new WheelEntity(new WheelShapeProperties(Props.Name + " model", Props.Mass, Props.Radius)
            {
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
    }
}


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Microsoft.Robotics.Simulation.Engine;
//using Microsoft.Robotics.PhysicalModel;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Robotics.Simulation.Physics;
//using Microsoft.Dss.Core.Attributes;
//using Microsoft.Robotics.Simulation;
//using Microsoft.Ccr.Core;

//namespace Brumba.Simulation.SimulatedAckermanFourWheels
//{
//    [DataContract]
//    public class CompositeWheelProperties
//    {
//        [DataMember]
//        public string Name { get; set; }
//        [DataMember]
//        public string PhysicalMesh { get; set; }
//        [DataMember]
//        public string VisualMesh { get; set; }

//        [DataMember]
//        public Vector3 Position { get; set; }
//        [DataMember]
//        public float Mass { get; set; }
//        [DataMember]
//        public float Radius { get; set; }
//        [DataMember]
//        public float MaxSteerAngle { get; set; }

//        [DataMember]
//        public bool Motorized { get; set; }
//        [DataMember]
//        public bool Steerable { get; set; }
//        [DataMember]
//        public bool Flipped { get; set; }
//    }

//    [DataContract]
//    public class CompositeWheel : SimplifiedConvexMeshEnvironmentEntity
//    {
//        [DataMember]
//        public CompositeWheelProperties Props { get; set; }

//        public WheelWithHandle Model { get; set; }

//        public CompositeWheel()
//            : base()
//        {
//        }

//        public CompositeWheel(CompositeWheelProperties props)
//            : base(props.Position, props.PhysicalMesh, null)
//        {
//            Props = props;

//            State.Name = Props.Name + " body";
//            State.MassDensity.Mass = Props.Mass;
//            State.Assets.Mesh = Props.PhysicalMesh;
//            Material = new MaterialProperties("tire", 0.0f, 0.5f, 10.0f);
//            Flags = VisualEntityProperties.DisableRendering;
//        }

//        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
//        {
//            //Build body
//            //if (!Parent.Children.Any(ve => ve.State.Name == (Props.Name + " body")))
//            //{
//            //    Build();
//            //}
//            //Deserialize body
//            //else
//            //{
//            //    Body = Parent.Children.Where(ve => ve.State.Name == (Props.Name + " body")).Single();
//            //}
//            Model = new WheelWithHandle(Props);

//            var connector1 = new EntityJointConnector(Model, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3()) { EntityName = Model.State.Name };
//            var connector2 = new EntityJointConnector(this, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3()) { EntityName = State.Name };

//            Model.ParentJoint = new Joint
//            {
//                State = new JointProperties(new JointAngularProperties { TwistMode = JointDOFMode.Locked, Swing1Mode = JointDOFMode.Locked, Swing2Mode = JointDOFMode.Locked }, connector1, connector2)
//                {
//                    Name = Parent.State.Name + " joint Q",
//                    EnableCollisions = false
//                }
//            };

//            InsertEntity(Model);
//            base.Initialize(device, physicsEngine);
//        }

//        public override void Update(FrameUpdate update)
//        {
//            var qq = Quaternion.ToAxisAngle(State.Pose.Orientation);
//            //Model.PhysicsEntity.SetShapeLocalPose(this.State.PhysicsPrimitives[0], new Pose(new Vector3(), Quaternion.FromAxisAngle(1, 0, 0, qq.Angle)));
//            PhysicsEntity.SetShapeLocalPose(Model.State.PhysicsPrimitives[1], new Pose(new Vector3(), Quaternion.FromAxisAngle(1, 0, 0, qq.Angle)));
//            //Parent.PhysicsEntity.SetShapeLocalPose(Model.State.PhysicsPrimitives[1], new Pose(State.Pose.Position, Quaternion.FromAxisAngle(1, 0, 0, (float)Math.PI / 2)));

//            base.Update(update);
//            // set our global pose to be the parents pose. Then our wheel shape will be
//            // positioned relative to that. We could also just set our global position to be
//            // parent pose combines with local pose, and then just use base.Render()
//            //if (Parent != null)
//            //    State.Pose = Parent.State.Pose;

//            // set the wheel orientation to match rotations and the steer angle
//            //var qq = Quaternion.ToAxisAngle(State.Pose.Orientation);
//            //_wheel.State.LocalPose.Orientation = Quaternion.FromAxisAngle(0, 1, 0, qq.Angle);
//            //_wheel.WheelState.LocalPose = new Pose(new Vector3(), Quaternion.FromAxisAngle(0, 1, 0, (float)Math.PI / 2));

//            //// update the rotations for the next frame
//            //Rotations += (float)(_wheel.AxleSpeed * update.ElapsedTime * (float)(-1.0 / (2.0 * Math.PI)));

//            //PhysicsEntity.SetPose(new Pose(new Vector3(1, 1, 1) + State.Pose.Position));
//        }

//        public float AxleSpeed
//        {
//            get
//            {
//                //return Model.Wheel.Wheel.AxleSpeed;
//                return 0;
//            }
//            set
//            {
//                //Model.Wheel.AxleSpeed = value;
//                //((PhysicsJoint)Body.ParentJoint).SetAngularDriveVelocity(new Vector3(0, value, 0));
//            }
//        }

//        public float SteerAngle
//        {
//            get
//            {
//                return Model.Wheel.Wheel.SteerAngle;
//            }
//            set
//            {
//                Model.Wheel.Wheel.SteerAngle = value;
//                ((PhysicsJoint)ParentJoint).SetAngularDriveOrientation(Quaternion.FromAxisAngle(new AxisAngle(new Vector3(1, 0, 0), -value)));
//            }
//        }

//        public void Build()
//        {
//            JointAngularProperties jointAngularProps = new JointAngularProperties { Swing1Mode = JointDOFMode.Free };

//            if (Props.Motorized)
//                jointAngularProps.SwingDrive = new JointDriveProperties(JointDriveMode.Velocity, new SpringProperties(), 1);

//            if (Props.Steerable)
//            {
//                jointAngularProps.TwistMode = JointDOFMode.Limited;
//                jointAngularProps.TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(1000000, 10000, 0), 100000000);
//                jointAngularProps.UpperTwistLimit = new JointLimitProperties(Props.MaxSteerAngle * 1.1f, 0, new SpringProperties());
//                jointAngularProps.LowerTwistLimit = new JointLimitProperties(-Props.MaxSteerAngle * 1.1f, 0, new SpringProperties());
//            }

//            var connector1 = new EntityJointConnector(this, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3()) { EntityName = State.Name };
//            var connector2 = new EntityJointConnector(Parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), Props.Position) { EntityName = Parent.State.Name };

//            ParentJoint = new Joint
//            {
//                State = new JointProperties(jointAngularProps, connector1, connector2)
//                {
//                    Name = Props.Name + " joint",
//                    EnableCollisions = false
//                }
//            };
//        }
//    }

//    [DataContract]
//    public class WheelWithHandle : SingleShapeEntity
//    {
//        [DataMember]
//        public CompositeWheelProperties Props { get; set; }

//        public WheelEntity Wheel { get; set; }

//        public WheelWithHandle()
//            : base()
//        {
//        }

//        public WheelWithHandle(CompositeWheelProperties props)
//            : base(new SphereShape(new SphereShapeProperties(props.Name + " handle", 0.001f, new Pose(), 0.001f)), new Vector3())
//        {
//            Props = props;

//            //State.Flags = EntitySimulationModifiers.DisableCollisions;
//            Flags = VisualEntityProperties.DisableRendering;
//            State.Assets.Mesh = Props.VisualMesh;
//        }

//        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
//        {
//            base.Initialize(device, physicsEngine);

//            //Build or rebuild model
//            Wheel = BuildWheel();
//            Wheel.Parent = this;
//            Wheel.Initialize(device, physicsEngine);
//        }

//        public override void Render(VisualEntity.RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
//        {
//            Wheel.Render(renderMode, transforms, currentCamera);
//            base.Render(renderMode, transforms, currentCamera);
//        }

//        public override void Update(FrameUpdate update)
//        {
//            Wheel.Update(update);
//            base.Update(update);
//        }

//        public override void Dispose()
//        {
//            Wheel.Dispose();
//            base.Dispose();
//        }

//        private WheelEntity BuildWheel()
//        {
//            var wheel = new WheelEntity(new WheelShapeProperties(Props.Name + " model", Props.Mass * 0.01f, Props.Radius)
//            {
//                TireLateralForceFunction =
//                {
//                    AsymptoteValue = 0.001f,
//                    StiffnessFactor = 1.0e8f
//                },
//                TireLongitudalForceFunction =
//                {
//                    AsymptoteValue = 0.001f,
//                    StiffnessFactor = 1.0e8f
//                }
//            })
//            {
//                State =
//                {
//                    Name = Props.Name + " model",
//                    Assets = { Mesh = Props.VisualMesh }
//                },
//            };
//            if (Props.Motorized)
//                wheel.WheelShape.WheelState.Flags |= WheelShapeBehavior.OverrideAxleSpeed;
//            if (Props.Flipped)
//                wheel.MeshRotation = new Vector3(0, 180, 0);
//            return wheel;
//        }
//    }
//}
