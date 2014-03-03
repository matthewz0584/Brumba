using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class RangefinderProperties
    {
        public float AngularResolution { get; set; }
        public float AngularRange { get; set; }
        public float MaxRange { get; set; }

        public Vector2 BeamToVectorInRobotTransformation(float zi, int i, float zeroBeamAngle)
        {
            return Vector2.Transform(new Vector2(zi, 0), Matrix.CreateRotationZ(zeroBeamAngle + i * AngularResolution));
        }
    }
}