using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brumba.Utils
{
    public class MathHelper2
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
    }
}
