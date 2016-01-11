using Brumba.Common;
using Brumba.Utils;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using Quaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.Simulation.Common
{
    public static class SimToMapTransformations
    {
        public static Vector3 MapToSim(Vector2 v, float height)
        {
            return new Vector3(v.Y, height, v.X);
        }

        public static Pose SimToMap(this Microsoft.Robotics.PhysicalModel.Pose v)
        {
            return new Pose(SimToMap(v.Position), SimToMap(v.Orientation));
        }

        public static Vector2 SimToMap(this Vector3 v)
        {
            return new Vector2(v.Z, v.X);
        }

        public static double SimToMap(this Quaternion q)
        {
            return (MathHelper.ToRadians(UIMath.QuaternionToEuler(q).Y) - MathHelper.Pi).ToPositiveAngle();
        }

        public static double SimToMapAngularVelocity(this Vector3 v)
        {
            return v.Y;
        }
    }
}