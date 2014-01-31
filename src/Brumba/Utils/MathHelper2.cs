using System;
using Microsoft.Xna.Framework;

namespace Brumba.Utils
{
    public static class MathHelper2
    {
        public static float ToPositiveAngle(float angle)
        {
            var angleRem = angle % TwoPi;
            return angleRem + (angleRem < 0 ? TwoPi : 0);
        }

        public static float AngleDifference(float angle2, float angle1)
        {
            var diff = Math.Abs(ToPositiveAngle(angle2) - ToPositiveAngle(angle1));
            return Math.Min(diff, TwoPi - diff);
        }

        public static float TwoPi { get { return 2 * (float)Math.PI; } }

	    public static bool EqualsWithin(double value, double testee, double margin)
	    {
			return Math.Abs(testee - value) / value <= Math.Abs(margin);
	    }

		public static bool EqualsWithin(this Vector2 me, Vector2 notMe, double margin)
		{
			return (notMe - me).Length() / me.Length() <= margin;
		}

		public static bool EqualsWithin(this Vector3 me, Vector3 notMe, double margin)
	    {
		    return (notMe - me).Length() / me.Length() <= margin;
	    }

		public static bool EqualsWithin(this Vector4 me, Vector4 notMe, double margin)
		{
			return (notMe - me).Length() / me.Length() <= margin;
		}
    }
}
