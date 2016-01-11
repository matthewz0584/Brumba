using System;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Dss.Core.Attributes;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    [DataContract]
    public partial class CompositeWheel : SimplifiedConvexMeshEnvironmentEntity
    {
        [DataMember]
        public CompositeWheelProperties Props { get; set; }

		[DataMember]
		public WheelEntity ModelInner { get; set; }
		[DataMember]
		public WheelEntity ModelOutter { get; set; }

        public CompositeWheel()
        {
        }

        public CompositeWheel(CompositeWheelProperties props)
            : base(props.Position, props.PhysicalMesh, null)
        {
            Props = props;
        }

        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);
            PhysicsEntity.SolverIterationCount = 64;

            ModelInner.Parent = this;
            ModelInner.Initialize(device, physicsEngine);

            var mesh = SimulationEngine.ResourceCache.CreateMeshFromFile(device, Props.VisualMesh);
            var visualMeshScale = Props.Radius / mesh.BoundingBox.Max.Y;
            ModelOutter.MeshScale = new Vector3(visualMeshScale, visualMeshScale, visualMeshScale);
            ModelOutter.Parent = this;
			ModelOutter.Initialize(device, physicsEngine);
        }

        public override void Update(FrameUpdate update)
        {
            ModelInner.Update(update);
			ModelOutter.Update(update);
            base.Update(update);
        }

        public override void Render(RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
			ModelOutter.Render(renderMode, transforms, currentCamera);
            base.Render(renderMode, transforms, currentCamera);
        }

        public override void Dispose()
        {
            ModelInner.Dispose();
			ModelOutter.Dispose();
            base.Dispose();
        }

        public float AxleSpeed
        {
            get
            {
                return ModelInner.Wheel.AxleSpeed;
            }
            set
            {
                ModelInner.Wheel.AxleSpeed = value;
				ModelOutter.Wheel.AxleSpeed = value;
            }
        }

        public float SteeringAngle
        {
            get
            {
                var localOrientation = Quaternion.ToAxisAngle(State.Pose.Orientation * Quaternion.Inverse(Parent.State.Pose.Orientation));
				if (double.IsNaN(localOrientation.Angle))
					return 0;
                return Math.Sign(localOrientation.Axis.Y) * localOrientation.Angle;
            }
            set
            {
                //if WriteLocks show up DeferredTaskQueue.Post(new Task()) should be used
                ((PhysicsJoint)ParentJoint).SetAngularDriveOrientation(Quaternion.FromAxisAngle(new AxisAngle(new Vector3(1, 0, 0), -value)));
            }
        }
    }
}