using System;
using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;

namespace Brumba.Utils
{
    public static class MathHelper2
    {
        public static float ToPositiveAngle(float angle)
        {
            Contract.Ensures(Contract.Result<float>() >= 0);
            Contract.Ensures(Contract.Result<float>() <= TwoPi);

            var angleRem = angle % TwoPi;
            return angleRem + (angleRem < 0 ? TwoPi : 0);
        }

        public static float AngleDifference(float angle2, float angle1)
        {
            Contract.Ensures(Contract.Result<float>() >= 0);
            Contract.Ensures(Contract.Result<float>() <= TwoPi);

            var diff = Math.Abs(ToPositiveAngle(angle2) - ToPositiveAngle(angle1));
            return Math.Min(diff, TwoPi - diff);
        }

        public static float TwoPi { get { return 2 * (float)Math.PI; } }

	    public static bool EqualsWithin(double value, double testee, double margin)
	    {
            Contract.Requires(value != 0);

			return Math.Abs(testee - value) / value <= Math.Abs(margin);
	    }

		public static bool EqualsWithin(this Vector2 me, Vector2 notMe, double margin)
		{
            Contract.Requires(me.Length() != 0);

			return (notMe - me).Length() / me.Length() <= margin;
		}

		public static bool EqualsWithin(this Vector3 me, Vector3 notMe, double margin)
	    {
            Contract.Requires(me.Length() != 0);

		    return (notMe - me).Length() / me.Length() <= margin;
	    }

		public static bool EqualsWithin(this Vector4 me, Vector4 notMe, double margin)
		{
            Contract.Requires(me.Length() != 0);

			return (notMe - me).Length() / me.Length() <= margin;
		}
    }
}
