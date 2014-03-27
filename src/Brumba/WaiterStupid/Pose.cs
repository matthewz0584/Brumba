using Brumba.Utils;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.WaiterStupid
{
	[DataContract]
    public struct Pose : IFreezable
    {
        Vector2 _position;
        double _bearing;

	    //_freezed will be set to true only from explicit ctor, then modification will not be allowed
        public Pose(Vector2 position, double bearing)
        {
            _position = position;
            _bearing = bearing;

            _freezed = true;
        }

		[DataMember]
		public Vector2 Position
        {
            get { return _position; }
			set { DC.Contract.Requires(!Freezed); _position = value; }
        }

        [DataMember]
		public double Bearing
        {
            get { return _bearing; }
	        set { DC.Contract.Requires(!Freezed); _bearing = value; }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Position, Bearing);
        }

        public static Pose operator -(Pose lhs, Pose rhs)
        {
            return new Pose(lhs.Position - rhs.Position, lhs.Bearing - rhs.Bearing);
        }

        bool _freezed;

        public void Freeze()
        {
            _freezed = true;
        }

	    public bool Freezed
	    {
	        get { return _freezed; }
	    }
    }
}