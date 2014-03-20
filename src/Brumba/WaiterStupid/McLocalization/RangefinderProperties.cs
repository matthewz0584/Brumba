using System.Diagnostics.Contracts;
using MathNet.Numerics;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public struct RangefinderProperties
    {
        public double AngularResolution { get; set; }
        public double AngularRange { get; set; }
        public double MaxRange { get; set; }
        public double ZeroBeamAngleInRobot { get; set; }

        public Vector2 BeamToVectorInRobotTransformation(float zi, int i)
        {
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= MaxRange);
            Contract.Requires(i >= 0);
            Contract.Requires(i < AngularRange / AngularResolution + 1);
            Contract.Requires(ZeroBeamAngleInRobot >= 0);
            Contract.Requires(ZeroBeamAngleInRobot < MathHelper.TwoPi);
            Contract.Requires(AngularResolution > 0);
            Contract.Ensures(Contract.Result<Vector2>().Length().AlmostEqualInDecimalPlaces(zi, 5));

            return Vector2.Transform(new Vector2(zi, 0), Matrix.CreateRotationZ((float)ZeroBeamAngleInRobot + i * (float)AngularResolution));
        }
    }
}