using System.Diagnostics.Contracts;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public struct Pose
    {
        readonly Vector2 _position;
        readonly double _bearing;

        public Pose(Vector2 position, double bearing)
        {
            Contract.Requires(bearing.Between(0, Constants.Pi2));

            _position = position;
            _bearing = bearing;
        }

        public Vector2 Position
        {
            get { return _position; }
        }

        public double Bearing
        {
            get { return _bearing; }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Position, Bearing);
        }
    }
}