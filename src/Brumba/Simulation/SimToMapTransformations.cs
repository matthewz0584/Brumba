using Brumba.Utils;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using rQuaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using rVector3 = Microsoft.Robotics.PhysicalModel.Vector3;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using xVector2 = Microsoft.Xna.Framework.Vector2;
using bPose = Brumba.WaiterStupid.Pose;

namespace Brumba.Simulation
{
    public static class SimToMapTransformations
    {
        public static rVector3 MapToSim(xVector2 v, float height)
        {
            return new rVector3(v.Y, height, v.X);
        }

        public static bPose SimToMap(this rPose v)
        {
            return new bPose(SimToMap(v.Position), SimToMap(v.Orientation));
        }

        public static xVector2 SimToMap(this rVector3 v)
        {
            return new xVector2(v.Z, v.X);
        }

        public static double SimToMap(this rQuaternion q)
        {
            return (MathHelper.ToRadians(UIMath.QuaternionToEuler(q).Y) - MathHelper.Pi).ToPositiveAngle();
        }

        public static double SimToMapAngularVelocity(this rVector3 v)
        {
            return v.Y;
        }
    }
}