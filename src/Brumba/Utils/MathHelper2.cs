using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Brumba.Utils
{
    public static class MathHelper2
    {
        public static float ToPositiveAngle(this float angle)
        {
            Contract.Requires(!float.IsInfinity(angle));
            Contract.Ensures(float.IsNaN(angle) || Contract.Result<float>() >= 0);
            Contract.Ensures(float.IsNaN(angle) || Contract.Result<float>() < MathHelper.TwoPi);

            var angleRem = angle % MathHelper.TwoPi;
            return angleRem + (angleRem < 0 ? MathHelper.TwoPi : 0);
            //var angleMinAbs = angle.ToMinAbsValueAngle();
            //return angleMinAbs + (angleMinAbs < 0 ? MathHelper.TwoPi : 0);
        }

	    public static float ToMinAbsValueAngle(this float angle)
	    {
            Contract.Requires(!float.IsInfinity(angle));
            Contract.Ensures(float.IsNaN(angle) || Contract.Result<float>() >= -MathHelper.Pi);
            Contract.Ensures(float.IsNaN(angle) || Contract.Result<float>() < MathHelper.Pi);

            var posAngle = angle.ToPositiveAngle();
            return (MathHelper.TwoPi - posAngle) <= posAngle ? posAngle - MathHelper.TwoPi : posAngle;
            //I do not use WrapAngle because it returns angles -pi < a <= pi, and I need -pi included, and pi excluded
	        //return MathHelper.WrapAngle(angle);
	    }

        public static float AngleDifference(float angle1, float angle2)
        {
            Contract.Requires(!float.IsInfinity(angle2));
            Contract.Requires(!float.IsInfinity(angle1));
            Contract.Ensures(float.IsNaN(angle1) || float.IsNaN(angle2) || Contract.Result<float>() >= 0);
            Contract.Ensures(float.IsNaN(angle1) || float.IsNaN(angle2) || Contract.Result<float>() <= MathHelper.TwoPi);

            var diff = Math.Abs(angle1.ToPositiveAngle() - angle2.ToPositiveAngle());
            return Math.Min(diff, MathHelper.TwoPi - diff);
        }

        public static bool EqualsRelatively(this double me, double value, double relativeError)
	    {
            Contract.Requires(relativeError >= 0);

            return Math.Abs(me - value) <= relativeError * Math.Abs(value);
	    }

        public static bool EqualsRelatively(this float me, float value, float relativeError)
        {
            Contract.Requires(relativeError >= 0);

            return Math.Abs(me - value) <= relativeError * Math.Abs(value);
        }

        public static bool EqualsRelatively(this Vector2 me, Vector2 notMe, double relativeError)
		{
            Contract.Requires(relativeError >= 0);

            return (notMe - me).LengthSquared() <= relativeError * relativeError * me.LengthSquared();
		}

        public static bool EqualsRelatively(this Vector3 me, Vector3 notMe, double relativeError)
	    {
            Contract.Requires(relativeError >= 0);

            return (notMe - me).LengthSquared() <= relativeError * relativeError * me.LengthSquared();
	    }

        public static bool EqualsRelatively(this Vector4 me, Vector4 notMe, double relativeError)
		{
            Contract.Requires(relativeError >= 0);

            return (notMe - me).LengthSquared() <= relativeError * relativeError * me.LengthSquared();
		}

        [Pure]
        public static bool Between(this float me, float lower, float upper)
        {
            Contract.Requires(upper > lower);

            return me >= lower && upper > me;
        }

        [Pure]
        public static bool Between(this int me, int lower, int upper)
        {
            Contract.Requires(upper > lower);

            return me >= lower && upper > me;
        }

        [Pure]
        public static bool Between(this Vector2 me, Vector2 lower, Vector2 upper)
        {
            Contract.Requires(upper.X > lower.X);
            Contract.Requires(upper.Y > lower.Y);

            return me.X.Between(lower.X, upper.X) && me.Y.Between(lower.Y, upper.Y);
        }

        [Pure]
        public static bool Between(this Vector3 me, Vector3 lower, Vector3 upper)
        {
            Contract.Requires(upper.X > lower.X);
            Contract.Requires(upper.Y > lower.Y);
            Contract.Requires(upper.Z > lower.Z);

            return me.X.Between(lower.X, upper.X) && me.Y.Between(lower.Y, upper.Y) && me.Z.Between(lower.Z, upper.Z);
        }

        [Pure]
        public static bool Between(this Point me, Point lower, Point upper)
        {
            Contract.Requires(upper.X > lower.X);
            Contract.Requires(upper.Y > lower.Y);

            return me.X.Between(lower.X, upper.X) && me.Y.Between(lower.Y, upper.Y);
        }

        [Pure]
        public static bool GreaterOrEqual(this Vector3 me, Vector3 notMe)
        {
            return me.X >= notMe.X && me.Y >= notMe.Y && me.Z >= notMe.Z;
        }

        public static Vector2 ExtractVector2(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static float AngleMean(IEnumerable<float> angles)
        {
            Contract.Requires(angles != null);
            Contract.Requires(angles.Any());
            Contract.Ensures(Single.IsNaN(Contract.Result<float>()) || Contract.Result<float>().Between(0, MathHelper.TwoPi));

            var avgVector = angles.Aggregate(new Vector2(), (sum, next) => new Vector2((float)Math.Cos(next), (float)Math.Sin(next)) + sum);
            return avgVector.LengthSquared() < 0.01 ?
                Single.NaN :
                ((float)Math.Atan2(avgVector.Y, avgVector.X)).ToPositiveAngle();
        }
    }
}
