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
            public Vector3 TailCenter { get; set; }
            public float TailMass { get; set; }
            public float TailMassRadius { get; set; }
			//Clockwise from left front rangefinder
			public Vector3[] GroundRangefindersPositions { get; set; }
			public float ScanInterval { get; set; }

            public StabilizerEntity Build(string name, VisualEntity parent)
            {
                var se = new StabilizerEntity(name, this);
                
                se.State.Pose.Position = TailCenter;

                var jointAngularProps = new JointAngularProperties
                    {
                        TwistMode = JointDOFMode.Free,
                        TwistDrive = new JointDriveProperties(JointDriveMode.Velocity, new SpringProperties(100000, 1000, 0), 10000),
                    };

                var jointLinearProps = new JointLinearProperties
                    {
                        YMotionMode = JointDOFMode.Limited,
                        XMotionMode = JointDOFMode.Free,
                        YDrive = new JointDriveProperties(JointDriveMode.Velocity, new SpringProperties(1000, 10f, 0), 10000),
                        XDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(100000, 10000, 0), 10000),                        
                        MotionLimit = new JointLimitProperties(0.5f, 1, new SpringProperties(10000000, 1000, 0))
                    };

                var connector1 = new EntityJointConnector(se, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3()) { EntityName = se.State.Name };
                //var connector2 = new EntityJointConnector(parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), TailCenter + new Vector3(0.3f, 0, 0)) { EntityName = parent.State.Name };
                var connector2 = new EntityJointConnector(parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), TailCenter) { EntityName = parent.State.Name };

                se.ParentJoint = new Joint
                {
                    State = new JointProperties(jointLinearProps, connector1, connector2)
                    {
                        Angular = jointAngularProps,
                        Name = name + " joint"
                    }
                };

				for (var i = 0; i < GroundRangefindersPositions.Length; ++i)
				{
					se.GroundRangefinders.Add(new InfraredRfEntity(String.Format("{0} rangefinder#{1}", name, i),
														new Pose(GroundRangefindersPositions[i], Quaternion.FromAxisAngle(1, 0, 0, (float)Math.PI / 2))) { ScanInterval = ScanInterval });
					parent.InsertEntity(se.GroundRangefinders.Last());
				}

                return se;
            }
        }

        private StabilizerProperties _props;

        public StabilizerEntity(string name, StabilizerProperties props)
        {
            _props = props;

            SphereShape = new SphereShape(new SphereShapeProperties(_props.TailMass, new Pose(), _props.TailMassRadius));
            State.Name = name;
			GroundRangefinders = new List<InfraredRfEntity>();
        }

		public List<InfraredRfEntity> GroundRangefinders { get; private set; }

        public float TailLinearVelocity
        {
            get { return 0; }
            set { ((PhysicsJoint) ParentJoint).SetLinearDriveVelocity(new Vector3(0, value, 0)); }
            //set { ((PhysicsJoint)ParentJoint).SetLinearDrivePosition(new Vector3(0, value, 0)); }
        }

        public float TailAngularVelocity
        {
            get { return 0; }
            set { ((PhysicsJoint)ParentJoint).SetAngularDriveVelocity(new Vector3(value, value, value)); }
        }

        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);
            //PhysicsEntity.SolverIterationCount = 32;
            //Parent.PhysicsEntity.SolverIterationCount = 32;
            TailLinearVelocity = 0.1f;
            //TailAngularVelocity = 10f;
        }

        public override void Update(FrameUpdate update)
        {
            //this.Position = new Microsoft.Xna.Framework.Vector3(0, 1, 0);
            //TailLinearVelocity = 0.9f;
            //TailAngularVelocity = 0.1f;
            base.Update(update);
        }
    }
}
