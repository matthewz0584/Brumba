using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Quaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.Simulation.SimulatedTurret
{
    [DataContract]
    public class TurretEntity : SingleShapeEntity
    {
        public class Builder
        {
            private readonly TurretProperties _props;

            public Builder(TurretProperties props)
            {
                _props = props;
            }

            public TurretEntity Build(string name, Pose pose, VisualEntity parent)
            {
                var turret = new TurretEntity(name, pose, _props);
                Build(turret);
                return turret;
            }

            public void Build(TurretEntity turret)
            {
                turret.CapsuleShape = new CapsuleShape(
                    new CapsuleShapeProperties(_props.BaseMass, new Pose(),
                                               _props.SegmentRadius,
                                               _props.BaseHeight) {DiffuseColor = new Vector4(0, 0, 0, 0)});

                turret.ParentJoint = BuildTwistJoint(turret.Parent, turret, new Vector3(1, 0, 0), new Vector3(0, 1, 0),
                                                     turret.State.Pose.Position - new Vector3(0, _props.BaseHeight/2, 0),
                                                     new Vector3(0, -_props.BaseHeight/2, 0),
                                                     _props.TwistPower);
            }

            static Joint BuildTwistJoint(VisualEntity parent, VisualEntity child, Vector3 axe1, Vector3 axe2, Vector3 parentPoint, Vector3 childPoint, float power)
            {
                var jointAngularProps = new JointAngularProperties
                    {
                        TwistMode = JointDOFMode.Free,
                        TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(power, power / 10, 0), 10000),
                        Swing1Mode = JointDOFMode.Free,
                        Swing2Mode = JointDOFMode.Free,
                        SwingDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(1000000, 10000, 0), 10000)
                    };
                var connector1 = new EntityJointConnector(parent, axe1, axe2, parentPoint) { EntityName = parent.State.Name };
                var connector2 = new EntityJointConnector(child, axe1, axe2, childPoint) { EntityName = child.State.Name };
                return new Joint
                    {
                        State = new JointProperties(jointAngularProps, connector1, connector2)
                                { Name = parent.State.Name + " " + child.State.Name + " joint" }
                    };
            }
        }

        [DataMember]
        public TurretProperties Props { get; set; }

        public TurretEntity()
        {}

        public TurretEntity(string name, Pose pose, TurretProperties props)
        {
            Props = props;

            State.Name = name;
            State.Pose = pose;
        }

        public SingleShapeEntity EndSegment { get; set; }

		float _baseAngle;
		public float BaseAngle
		{
			get { return _baseAngle; }
			set
			{
				_baseAngle = value;
				((PhysicsJoint)ParentJoint).SetAngularDriveOrientation(Quaternion.FromAxisAngle(1, 0, 0, value));
			}
		}

        public override void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            if (Props != null && EndSegment == null)
                new Builder(Props).Build(this);
            
            EndSegment = this;

            base.Initialize(device, physicsEngine);
        }
    }
}
