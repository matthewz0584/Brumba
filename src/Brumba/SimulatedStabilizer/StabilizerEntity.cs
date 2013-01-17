using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;

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

                se.ParentJoint = BuildJoint(name, parent, se);

                var rfs = BuildGroundRangefinders(name);
                se.GroundRangefinders.AddRange(rfs);
                se.GroundRangefinders.ForEach(parent.InsertEntity);

                return se;
            }

            Joint BuildJoint(string name, VisualEntity parent, StabilizerEntity se)
            {
                var jointLinearProps = new JointLinearProperties
                    {
                        YMotionMode = JointDOFMode.Limited,
                        ZMotionMode = JointDOFMode.Limited,
                        XMotionMode = JointDOFMode.Free,
                        YDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(100, 10f, 0), 10000),
                        ZDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(100, 10f, 0), 10000),
                        XDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(100000, 10000, 0), 10000),
                        MotionLimit = new JointLimitProperties(0.5f, 1, new SpringProperties(10000, 100, 0))
                    };

                var connector1 = new EntityJointConnector(se, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3())
                    { EntityName = se.State.Name };
                var connector2 = new EntityJointConnector(parent, new Vector3(1, 0, 0), new Vector3(0, 1, 0), TailCenter)
                    { EntityName = parent.State.Name };

                return new Joint
                    {
                        State = new JointProperties(jointLinearProps, connector1, connector2) {Name = name + " joint"}
                    };
            }

            IEnumerable<InfraredRfEntity> BuildGroundRangefinders(string stabilizerName)
            {
                return GroundRangefindersPositions.Select((v, i) =>
                    new InfraredRfEntity(String.Format("{0} rangefinder#{1}", stabilizerName, i),
                        new Pose(GroundRangefindersPositions[i], Quaternion.FromAxisAngle(1, 0, 0, Microsoft.Xna.Framework.MathHelper.PiOver2)))
                        { ScanInterval = ScanInterval });
            }
        }

        private StabilizerProperties _props;
        private Vector2 _tailPosition;

        public StabilizerEntity(string name, StabilizerProperties props)
        {
            _props = props;

            SphereShape = new SphereShape(new SphereShapeProperties(_props.TailMass, new Pose(), _props.TailMassRadius));
            State.Name = name;
			GroundRangefinders = new List<InfraredRfEntity>();
        }

		public List<InfraredRfEntity> GroundRangefinders { get; private set; }

        public Vector2 TailPosition
        {
            get { return _tailPosition; }
            set
            {
                _tailPosition = value;
                ((PhysicsJoint)ParentJoint).SetLinearDrivePosition(new Vector3(0, _tailPosition.X, _tailPosition.Y));
            }
        }

        public override void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);
            TailPosition = new Vector2(0.4f, 0.4f);
        }
    }
}
