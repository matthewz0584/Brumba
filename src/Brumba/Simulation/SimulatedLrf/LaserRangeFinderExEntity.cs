using System;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using xna = Microsoft.Xna.Framework;

namespace Brumba.Simulation.SimulatedLrf
{
	[DataContract]
	public class LaserRangeFinderExEntity : LaserRangeFinderEntity
	{
        RaycastProperties _raycastProperties_FORDB;
		[DataMember]
		public RaycastProperties RaycastProperties_FORDB
		{
            get { return _raycastProperties_FORDB; }
            set { RaycastProperties = _raycastProperties_FORDB = value; }
		}

	    public Port<RaycastResult> ServiceNotification { get; private set; }

        public override void Update(FrameUpdate update)
        {
			//Cheat for base.Update: i need only call to VisualEntity.Update to happen, nothing more
			//LaserRangeFinderEntity.Update method {
			//using (Profiler.AutoPop autoPop = Profiler.PushAutoPopSection("LaserRangeFinderEntity.Update(FrameUpdate update)", Profiler.SectionType.Update))
			//{
			//	if (_raycastProperties == null)
			//	{
			//		base.Update(update);
			//		return;
			//	}
			var curRp = RaycastProperties;
			RaycastProperties = null;
			base.Update(update);
			RaycastProperties = curRp;

            if (RaycastProperties == null)
                return;

            ApplicationTime = (float)update.ApplicationTime;

            ElapsedSinceLastScan += (float)update.ElapsedTime;
            // only retrieve raycast results every SCAN_INTERVAL.
            // For entities that are compute intenisve, you should consider giving them
            // their own task queue so they dont flood a shared queue
            if (ElapsedSinceLastScan <= RaycastProperties.ScanInterval)
                return;

            ElapsedSinceLastScan = 0;
            // the LRF looks towards the negative Z axis (towards the user), not the positive Z axis
            // which is the default orientation. So we have to rotate its orientation by 180 degrees

            RaycastProperties.OriginPose.Orientation = TypeConversion.FromXNA(
                TypeConversion.ToXNA(State.Pose.Orientation) * xna.Quaternion.CreateFromAxisAngle(new xna.Vector3(0, 1, 0), (float)Math.PI));

            // to calculate the position of the origin of the raycast, we must first rotate the LocalPose position
            // of the raycast (an offset from the origin of the parent entity) by the orientation of the parent entity.
            // The origin of the raycast is then this rotated offset added to the parent position.
            var parentOrientation = xna.Matrix.CreateFromQuaternion(TypeConversion.ToXNA(State.Pose.Orientation));
            var localOffset = xna.Vector3.Transform(TypeConversion.ToXNA(LaserBox.State.LocalPose.Position), parentOrientation);

            RaycastProperties.OriginPose.Position = State.Pose.Position + TypeConversion.FromXNA(localOffset);

            RaycastResultsPort = PhysicsEngine.Raycast2D(RaycastProperties);

            RaycastResult lastResults;
            RaycastResultsPort.Test(out lastResults);
            if (ServiceNotification != null && lastResults != null)
                ServiceNotification.Post(LastResults = lastResults);
        }

        public new void Register(Port<RaycastResult> notificationTarget)
        {
            if (notificationTarget == null)
                throw new ArgumentNullException("notificationTarget");
            if (ServiceNotification != null)
                throw new InvalidOperationException("A notification target is already registered");
            ServiceNotification = notificationTarget;
        }
	}
}