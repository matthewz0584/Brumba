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
        public class StabilizerProperties
        {
            public Vector3 TailPosition { get; set; }
            public float TailMass { get; set; }
            public float TailMassRadius { get; set; }
            public Vector3 LfWheelPosition { get; set; }
            public Vector3 RfWheelPosition { get; set; }
            public Vector3 LrWheelPosition { get; set; }
            public Vector3 RrWheelPosition { get; set; }

            public StabilizerEntity Build(string name, VisualEntity parent)
            {
                var se = new StabilizerEntity(name, this);
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

                //se.LfWheelRf = new InfraredRfEntity(name + " left front wheel rf",
                //                                    new Pose(new Vector3(0, 0, 0.5f),
                //                                             Quaternion.FromAxisAngle(1, 0, 0, (float)Math.PI / 2)));
                //parent.InsertEntity(se.LfWheelRf);
                se.RfWheelRf = new InfraredRfEntity(name + " right front wheel rf",
                                                    new Pose(RfWheelPosition, Quaternion.FromAxisAngle(1, 0, 0, (float)Math.PI / 2)));
                se.InsertEntity(se.RfWheelRf);
                se.LfWheelRf = new InfraredRfEntity(name + " left front wheel rf",
                                                    new Pose(LfWheelPosition, Quaternion.FromAxisAngle(1, 0, 0, (float)Math.PI / 2)));
                se.InsertEntity(se.LfWheelRf);

                return se;
            }
        }

        private StabilizerProperties _props;

        public StabilizerEntity(string name, StabilizerProperties props)
        {
            _props = props;

            SphereShape = new SphereShape(new SphereShapeProperties(_props.TailMass, new Pose(), _props.TailMassRadius));
            State.Name = name;
            State.Pose.Position = _props.TailPosition;
        }

        public InfraredRfEntity LfWheelRf { get; private set; }
        public InfraredRfEntity RfWheelRf { get; private set; }

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
