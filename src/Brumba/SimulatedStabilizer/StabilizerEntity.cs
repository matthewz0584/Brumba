using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Xna = Microsoft.Xna.Framework;

namespace Brumba.Simulation.SimulatedStabilizer
{
    public class StabilizerEntity : SingleShapeEntity
    {
        public class StabilizerProperties
        {
            public Vector3 TailCenter { get; set; }
            public float TailMass { get; set; }
            public float TailMassRadius { get; set; }
            public float TailMaxShoulder { get; set; }
            public float TailPower { get; set; }
			//Clockwise from left front rangefinder
			public Vector3[] GroundRangefindersPositions { get; set; }
			public float ScanInterval { get; set; }

            public StabilizerEntity BuildAndInsert(string name, VisualEntity parent)
            {
                var se = new StabilizerEntity(name, this);
                
                se.State.Pose.Position = TailCenter;

                se.ParentJoint = BuildJoint(name, parent, se);

                var rfs = BuildGroundRangefinders(name);
                se.GroundRangefinders.AddRange(rfs);
                se.GroundRangefinders.ForEach(parent.InsertEntity);

				parent.InsertEntity(se);

                return se;
            }

            Joint BuildJoint(string name, VisualEntity parent, StabilizerEntity se)
            {
	            var jointAngularProps = new JointAngularProperties
		            {
			            TwistMode = JointDOFMode.Locked,
			            TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(1, 0, 0), 10000)
		            };

                var jointLinearProps = new JointLinearProperties
                    {
                        YMotionMode = JointDOFMode.Locked,
//                        ZMotionMode = JointDOFMode.Limited,
//                        XMotionMode = JointDOFMode.Free,
                        YDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(TailPower, TailPower / 10f, 0), 10000),
//                        ZDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(TailPower, TailPower / 10f, 0), 10000),
//                        XDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(100000, 10000, 0), 10000),
                        MotionLimit = new JointLimitProperties(TailMaxShoulder, 1, new SpringProperties(10000, 100, 0))
                    };

	            var axe1 = new Vector3(1, 0, 0);
	            var axe2 = new Vector3(0, 0, 1);
				var connector1 = new EntityJointConnector(se, axe1, axe2, new Vector3(0, 0, 0)) { EntityName = se.State.Name };
                var connector2 = new EntityJointConnector(parent, axe1, axe2, TailCenter) { EntityName = parent.State.Name };

                return new Joint
                    {
                        State = new JointProperties(jointLinearProps, connector1, connector2)
	                        {
		                        Name = name + " joint",
								Angular = jointAngularProps
	                        }
                    };
            }

            IEnumerable<InfraredRfEntity> BuildGroundRangefinders(string stabilizerName)
            {
                return GroundRangefindersPositions.Select((v, i) =>
                    new InfraredRfEntity(String.Format("{0} rangefinder#{1}", stabilizerName, i),
                        new Pose(GroundRangefindersPositions[i], Quaternion.FromAxisAngle(1, 0, 0, Xna.MathHelper.PiOver2)))
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

	    /// <summary>
	    /// X - codirected with parent Z, Y - with parent X
	    /// </summary>
	    //public Vector2 TailPosition
	    //{
	    //	get { return _tailPosition; }
	    //	set
	    //	{
	    //		var xnaPos = new Xna.Vector2(value.X, value.Y);
	    //		var xnaPosClamped = xnaPos.Length() > _props.TailMaxShoulder
	    //								? Xna.Vector2.Normalize(xnaPos) * _props.TailMaxShoulder : xnaPos;
	    //		_tailPosition = new Vector2(xnaPosClamped.X, xnaPosClamped.Y);
	    //		((PhysicsJoint)ParentJoint).SetLinearDrivePosition(new Vector3(0, -_tailPosition.Y, _tailPosition.X));
	    //	}
	    //}
	    float _shoulder;
		public float TailShoulder
		{
			get { return _shoulder; }
			set
			{
				_shoulder = value;
				//((PhysicsJoint)ParentJoint).SetLinearDrivePosition(new Vector3(0, value, 0));
			}
		}

		float _angle;
		public float Angle
		{
			get { return _angle; }
			set
			{
				_angle = value;
				//((PhysicsJoint)ParentJoint).SetAngularDriveVelocity(new Vector3(value, 0, 0));
				//((PhysicsJoint)ParentJoint).SetAngularDriveOrientation(Quaternion.FromAxisAngle(1, 0, 0, value));
			}
		}

        public override void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);
        }
    }
}
