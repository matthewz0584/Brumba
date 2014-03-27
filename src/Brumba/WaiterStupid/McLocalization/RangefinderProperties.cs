using MathNet.Numerics;
using DC = System.Diagnostics.Contracts;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    [DataContract]
    public struct RangefinderProperties
    {
        [DataMember]
        public double AngularResolution { get; set; }
        [DataMember]
        public double AngularRange { get; set; }
        [DataMember]
        public double MaxRange { get; set; }
        [DataMember]
        public double ZeroBeamAngleInRobot { get; set; }

        public Vector2 BeamToVectorInRobotTransformation(float zi, int i)
        {
            DC.Contract.Requires(zi >= 0);
            DC.Contract.Requires(zi <= MaxRange);
            DC.Contract.Requires(i >= 0);
            DC.Contract.Requires(i < AngularRange / AngularResolution + 1);
            DC.Contract.Requires(ZeroBeamAngleInRobot >= 0);
            DC.Contract.Requires(ZeroBeamAngleInRobot < MathHelper.TwoPi);
            DC.Contract.Requires(AngularResolution > 0);
            DC.Contract.Ensures(DC.Contract.Result<Vector2>().Length().AlmostEqualInDecimalPlaces(zi, 5));

            return Vector2.Transform(new Vector2(zi, 0), Matrix.CreateRotationZ((float)ZeroBeamAngleInRobot + i * (float)AngularResolution));
        }
    }
}