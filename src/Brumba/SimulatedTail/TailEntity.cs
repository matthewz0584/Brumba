using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Quaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.Simulation.SimulatedTail
{
    public class TailEntity : SingleShapeEntity
    {
        public class TailProperties
        {
            public Vector3 Origin { get; set; }
            
            public float PayloadMass { get; set; }
            public float PayloadRadius { get; set; }
            
            public float TwistPower { get; set; }

            public float Segment1Length { get; set; }
            public float Segment1Mass { get; set; }

            public float Segment2Length { get; set; }
            public float Segment2Mass { get; set; }

            public float SegmentRadius { get; set; }

            //Clockwise from left front rangefinder
            public Vector3[] GroundRangefindersPositions { get; set; }
            public float ScanInterval { get; set; }

            public TailEntity Build(string name, VisualEntity parent)
            {
                var tailSegment1 =
                    new TailEntity(
                        SegmentShape(Segment1Mass, SegmentRadius, Segment1Length),
                        Origin + new Vector3(0, Segment1Length/2, 0), this)
                        {
                            State = {Name = name, Flags = EntitySimulationModifiers.IgnoreGravity}
                        };
                
                tailSegment1.ParentJoint = BuildTwistJoint(parent, tailSegment1, new Vector3(1, 0, 0), new Vector3(0, 0, 1), Origin, new Vector3(0, -Segment1Length / 2, 0), TwistPower);

                tailSegment1.Segment2 =
                    new SingleShapeEntity(
                        SegmentShape(Segment2Mass, SegmentRadius, Segment2Length),
                        new Vector3(0, Segment2Length/2, 0))
                        {
                            State = {Name = name + " segment#2", Flags = EntitySimulationModifiers.IgnoreGravity}
                        };
                tailSegment1.Segment2.ParentJoint = BuildTwistJoint(tailSegment1, tailSegment1.Segment2, new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, Segment1Length / 2, 0), new Vector3(0, -Segment2Length / 2, 0), TwistPower);

                tailSegment1.InsertEntity(tailSegment1.Segment2);

                tailSegment1.Payload =
                    new SingleShapeEntity(new SphereShape(new SphereShapeProperties(PayloadMass, new Pose(), PayloadRadius)),
                        new Vector3(0, Segment2Length / 2, 0))
                    {
                        State = { Name = name + " payload", Flags = EntitySimulationModifiers.IgnoreGravity }
                    };
                tailSegment1.Segment2.InsertEntity(tailSegment1.Payload);

                var rfs = BuildGroundRangefinders(name);
                tailSegment1.GroundRangefinders.AddRange(rfs);
                tailSegment1.GroundRangefinders.ForEach(parent.InsertEntity);

                return tailSegment1;
            }

            static CapsuleShape SegmentShape(float mass, float radius, float length)
            {
                return new CapsuleShape(new CapsuleShapeProperties(mass, new Pose(), radius, length));
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
                return new Joint { State = new JointProperties(jointAngularProps, connector1, connector2)
                                                { Name = parent.State.Name + " " + child.State.Name + " joint" } };
            }

            IEnumerable<InfraredRfEntity> BuildGroundRangefinders(string stabilizerName)
            {
                return GroundRangefindersPositions.Select((v, i) =>
                    new InfraredRfEntity(String.Format("{0} rangefinder#{1}", stabilizerName, i),
                        new Pose(GroundRangefindersPositions[i], Quaternion.FromAxisAngle(1, 0, 0, Microsoft.Xna.Framework.MathHelper.PiOver2)))
                        { ScanInterval = ScanInterval });
            }
        }

        private TailProperties _props;

        public TailEntity(CapsuleShape shape, Vector3 initialPos, TailProperties props)
            : base(shape, initialPos)
        {
            _props = props;
			GroundRangefinders = new List<InfraredRfEntity>();
        }

		public List<InfraredRfEntity> GroundRangefinders { get; private set; }
        public SingleShapeEntity Segment2 { get; private set; }
        public SingleShapeEntity Payload { get; private set; }

		float _segment1Angle;
		public float Segment1Angle
		{
			get { return _segment1Angle; }
			set
			{
				_segment1Angle = value;
				//((PhysicsJoint)ParentJoint).SetAngularDriveVelocity(new Vector3(value, 0, 0));
				((PhysicsJoint)ParentJoint).SetAngularDriveOrientation(Quaternion.FromAxisAngle(1, 0, 0, value));
			}
		}

        float _segment2Angle;
        public float Segment2Angle
        {
            get { return _segment2Angle; }
            set
            {
                _segment2Angle = value;
                //((PhysicsJoint)Segment2.ParentJoint).SetAngularDriveVelocity(new Vector3(value, value, value));
                ((PhysicsJoint)Segment2.ParentJoint).SetAngularDriveOrientation(Quaternion.FromAxisAngle(1, 0, 0, value));
            }
        }

        public override void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);
        }

        public override void Update(FrameUpdate update)
        {
            base.Update(update);
            //PhysicsEntity.SolverIterationCount = 128;
            //Segment2.PhysicsEntity.SolverIterationCount = 128;
            //Payload.PhysicsEntity.SolverIterationCount = 128;
        }
    }
}
