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

		public CompositeWheel Build(VisualEntity parent)
		{
			return SetParams(new CompositeWheel(this) { ParentJoint = BuildJoint(parent), Model = BuildModel() });
		}

		private CompositeWheel SetParams(CompositeWheel wheel)
		{
			wheel.Props = this;

			wheel.State.Name = Name + " body";
			wheel.State.MassDensity.Mass = Mass * 0.5f;
			wheel.State.Assets.Mesh = PhysicalMesh;
			wheel.Material = new MaterialProperties("tire", 0.0f, 0, 0);
			wheel.Flags = VisualEntityProperties.DisableRendering;
			return wheel;
		}

		private Joint BuildJoint(VisualEntity parent)
		{
			var jointAngularProps = new JointAngularProperties();

			if (Steerable)
			{
				jointAngularProps.TwistMode = JointDOFMode.Limited;
				jointAngularProps.TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(1000000, 10000, 0), 100000000);
				jointAngularProps.UpperTwistLimit = new JointLimitProperties(MaxSteerAngle * 1.1f, 0, new SpringProperties());
				jointAngularProps.LowerTwistLimit = new JointLimitProperties(-MaxSteerAngle * 1.1f, 0, new SpringProperties());
			}

			var jointLinearProps = new JointLinearProperties
			{
				XMotionMode = JointDOFMode.Free,
				XDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(500, 10, 0), 10000)
			};

			var connector1 = new EntityJointConnector(this, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3()) { EntityName = Name };
			var connector2 = new EntityJointConnector(parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), Position) { EntityName = parent.State.Name };

			return new Joint
			{
				State = new JointProperties(jointAngularProps, connector1, connector2)
				{
					Linear = jointLinearProps,
					Name = Name + " joint",
					EnableCollisions = false
				}
			};
		}

		private WheelEntity BuildModel()
		{
			var wheel = new WheelEntity(new WheelShapeProperties(Name + " model", Mass, Radius)
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
        }

        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);
            PhysicsEntity.SolverIterationCount = 64;

            Model.Parent = this;
            Model.Initialize(device, physicsEngine);
        }

        public override void Update(FrameUpdate update)
        {
            Model.Update(update);
            base.Update(update);
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
    }
}