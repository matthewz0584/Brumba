using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework.Graphics;

namespace Brumba.Simulation.SimulatedStabilizer
{
    public class StabilizerEntity : SingleShapeEntity
    {
        public static StabilizerEntity Build(string name, Vector3 position, float mass, float tailWeightRadius, VisualEntity parent)
        {
            var se = new StabilizerEntity(name, position + new Vector3(0.0f, 0.0f, 0), mass, tailWeightRadius);
            //var se = new StabilizerEntity(name, position, mass, tailWeightRadius);

            //var jointAngularProps = new JointAngularProperties
            //    {
            //        TwistMode = JointDOFMode.Free,
            //        TwistDrive = new JointDriveProperties(JointDriveMode.Velocity, new SpringProperties(10, 10, 0), 10000),
            //    };

            //var jointLinearProps = new JointLinearProperties
            //    {
            //        YMotionMode = JointDOFMode.Free,
            //        YDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(10, 10, 0), 10000),
            //        MotionLimit = new JointLimitProperties(1, 1, new SpringProperties())
            //    };

            //var connector1 = new EntityJointConnector(se, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3()) { EntityName = se.State.Name };
            //var connector2 = new EntityJointConnector(parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), position) { EntityName = parent.State.Name };

            //se.ParentJoint = new Joint
            //{
            //    State = new JointProperties(jointAngularProps, connector1, connector2)
            //    {
            //        Linear = jointLinearProps,
            //        Name = name + " joint"
            //    }
            //};

            se.Rf0 = new InfraredRfEntity(name + " rf0",
                                          new Pose(new Vector3(0, 0, 0.5f), Quaternion.FromAxisAngle(0, 1, 0, 2 * (float)Math.PI / 2)));
            //se.Rf0 = new IREntity(new Pose(new Vector3(0, -0.05f, 0.5f), Quaternion.FromAxisAngle(1, 0, 0, -(float)Math.PI / 2)), 0.0f, 0.8f, 0.0349f)
            //    {
            //        State = {Name = name + " rf1"},
            //        MinimumRange = 0.0f
            //    };
            //se.InsertEntity(se.Rf0);
            parent.InsertEntity(se.Rf0);

            return se;
        }

        public StabilizerEntity(string name, Vector3 position, float mass, float tailWeightRadius)
        {
            SphereShape = new SphereShape(new SphereShapeProperties(mass, new Pose(), tailWeightRadius));
            //State.MassDensity.Mass = mass;
            State.Name = name + " tail";
            State.Pose.Position = position;
        }

        public InfraredRfEntity Rf0 { get; private set; }

        public float TailLinearVelocity
        {
            get { return 0; }
            //set { ((PhysicsJoint) ParentJoint).SetLinearDriveVelocity(new Vector3(value, value, value)); }
            set { ((PhysicsJoint)ParentJoint).SetLinearDrivePosition(new Vector3(0, value, 0)); }
        }

        public float TailAngularVelocity
        {
            get { return 0; }
            set { ((PhysicsJoint)ParentJoint).SetAngularDriveVelocity(new Vector3(value, value, value)); }
        }

        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);
            TailLinearVelocity = 0.9f;
            //TailAngularVelocity = 0.1f;
        }

        public override void Update(FrameUpdate update)
        {
            //TailLinearVelocity = 0.9f;
            //TailAngularVelocity = 0.1f;
            base.Update(update);
        }
    }
}
