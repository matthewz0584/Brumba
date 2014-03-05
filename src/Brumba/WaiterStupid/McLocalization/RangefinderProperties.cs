using System.Diagnostics.Contracts;
using MathNet.Numerics;
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
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= MaxRange);
            Contract.Requires(i >= 0);
            Contract.Requires(zeroBeamAngle >= 0);
            Contract.Requires(zeroBeamAngle < MathHelper.TwoPi);
            Contract.Ensures(Contract.Result<Vector2>().Length().AlmostEqualInDecimalPlaces(zi, 5));

            return Vector2.Transform(new Vector2(zi, 0), Matrix.CreateRotationZ(zeroBeamAngle + i * AngularResolution));
        }
    }
}