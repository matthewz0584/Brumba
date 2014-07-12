using Microsoft.Xna.Framework;

namespace Brumba.Utils
{
    public static class PointExtensions
    {
        public static Point Plus(this Point lhs, Point rhs)
        {
            return new Point(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static Point Minus(this Point lhs, Point rhs)
        {
            return lhs.Plus(rhs.Scale(-1));
        }

        public static Point Scale(this Point lhs, int k)
        {
            return new Point(k * lhs.X, k * lhs.Y);
        }

        public static Point Perpendicular(this Point lhs)
        {
            return new Point(-lhs.Y, lhs.X);
        }

        public static int LengthSq(this Point lhs)
        {
            return lhs.X * lhs.X + lhs.Y * lhs.Y;
        }
    }
}