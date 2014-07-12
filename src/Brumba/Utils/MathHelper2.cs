using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Xna.Framework;

namespace Brumba.Utils
{
    public static class MathHelper2
    {
        public static double ToPositiveAngle(this double angle)
        {
            Contract.Requires(!double.IsInfinity(angle));
            Contract.Ensures(double.IsNaN(angle) || Contract.Result<double>() >= 0);
            Contract.Ensures(double.IsNaN(angle) || Contract.Result<double>() < Constants.Pi2);

	        var angleRem = angle % Constants.Pi2;
            if (angleRem > 0)
                return angleRem;
            //(angleRem + Constants.Pi2) >= Constants.Pi2 can't be true arithmetically
            //but due to precision errors it can be even simply greater, take it into account
            return (angleRem + Constants.Pi2) >= Constants.Pi2 ? 0 : (angleRem + Constants.Pi2);
        }

		public static float ToPositiveAngle(this float angle)
		{
			Contract.Requires(!float.IsInfinity(angle));
			Contract.Ensures(float.IsNaN(angle) || Contract.Result<float>() >= 0);
			Contract.Ensures(float.IsNaN(angle) || Contract.Result<float>() < MathHelper.TwoPi);

            var angleRem = angle % MathHelper.TwoPi;
            if (angleRem > 0)
                return angleRem;
            return (angleRem + MathHelper.TwoPi) >= MathHelper.TwoPi ? 0 : (angleRem + MathHelper.TwoPi);
		}

        public static double ToMinAbsValueAngle(this double angle)
	    {
            Contract.Requires(!double.IsInfinity(angle));
            Contract.Ensures(double.IsNaN(angle) || Contract.Result<double>() >= -Constants.Pi);
            Contract.Ensures(double.IsNaN(angle) || Contract.Result<double>() < Constants.Pi);

            var posAngle = angle.ToPositiveAngle();
            return (Constants.Pi2 - posAngle) <= posAngle ? posAngle - Constants.Pi2 : posAngle;
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

		public static double AngleDifference(double angle1, double angle2)
		{
			Contract.Requires(!double.IsInfinity(angle2));
			Contract.Requires(!double.IsInfinity(angle1));
			Contract.Ensures(double.IsNaN(angle1) || double.IsNaN(angle2) || Contract.Result<double>() >= 0);
			Contract.Ensures(double.IsNaN(angle1) || double.IsNaN(angle2) || Contract.Result<double>() <= Constants.Pi2);

			var diff = Math.Abs(angle1.ToPositiveAngle() - angle2.ToPositiveAngle());
			return Math.Min(diff, Constants.Pi2 - diff);
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
        public static bool Between(this double me, double lower, double upper)
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

        [Pure]
        public static bool GreaterOrEqual(this Vector2 me, Vector2 notMe)
        {
            return me.X >= notMe.X && me.Y >= notMe.Y;
        }

        public static Vector2 ExtractVector2(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static double AngleMean(IEnumerable<double> angles)
        {
            Contract.Requires(angles != null);
            Contract.Requires(angles.Any());
            Contract.Ensures(double.IsNaN(Contract.Result<double>()) || Contract.Result<double>().Between(0, Constants.Pi2));

            var avgVector = angles.Aggregate(new DenseVector(2), (sum, next) => new DenseVector(new []{Math.Cos(next), Math.Sin(next)}) + sum);
            return avgVector.DotProduct(avgVector) < 0.01 ?
                double.NaN :
                Math.Atan2(avgVector[1], avgVector[0]).ToPositiveAngle();
        }

	    [Pure]
        public static Vector2 ToVec(this Point me)
	    {
		    return new Vector2(me.X, me.Y);
	    }
    }
}
