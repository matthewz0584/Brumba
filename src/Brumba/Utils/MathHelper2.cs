using System;
using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;

namespace Brumba.Utils
{
    public static class MathHelper2
    {
        public static float ToPositiveAngle(float angle)
        {
            Contract.Requires(!float.IsInfinity(angle));
            Contract.Ensures(float.IsNaN(angle) || Contract.Result<float>() >= 0);
            Contract.Ensures(float.IsNaN(angle) || Contract.Result<float>() <= TwoPi);

            var angleRem = angle % TwoPi;
            return angleRem + (angleRem < 0 ? TwoPi : 0);
        }

        public static float AngleDifference(float angle1, float angle2)
        {
            Contract.Requires(!float.IsInfinity(angle2));
            Contract.Requires(!float.IsInfinity(angle1));
            Contract.Ensures(float.IsNaN(angle1) || float.IsNaN(angle2) || Contract.Result<float>() >= 0);
            Contract.Ensures(float.IsNaN(angle1) || float.IsNaN(angle2) || Contract.Result<float>() <= TwoPi);

            var diff = Math.Abs(ToPositiveAngle(angle1) - ToPositiveAngle(angle2));
            return Math.Min(diff, TwoPi - diff);
        }

        public static float TwoPi
        {
            get { return 2*(float) Math.PI; }
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
        public static bool Between(this Vector2 me, Vector2 lower, Vector2 upper)
        {
            Contract.Requires(upper.X > lower.X);
            Contract.Requires(upper.Y > lower.Y);

            return me.X >= lower.X && me.Y >= lower.Y && 
                upper.X > me.X && upper.Y > me.Y;
        }

        [Pure]
        public static bool Between(this Vector3 me, Vector3 lower, Vector3 upper)
        {
            Contract.Requires(upper.X > lower.X);
            Contract.Requires(upper.Y > lower.Y);
            Contract.Requires(upper.Z > lower.Z);

            return me.X >= lower.X && me.Y >= lower.Y && me.Z >= lower.Z &&
                    upper.X > me.X && upper.Y > me.Y && upper.Z > me.Z;
        }

        [Pure]
        public static bool Between(this Point me, Point lower, Point upper)
        {
            Contract.Requires(upper.X > lower.X);
            Contract.Requires(upper.Y > lower.Y);

            return me.X >= lower.X && me.Y >= lower.Y &&
                upper.X > me.X && upper.Y > me.Y;
        }

        public static Vector2 ExtractVector2(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }
    }
}
