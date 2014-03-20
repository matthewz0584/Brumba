using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.WaiterStupid.McLocalization
{
	[DataContract]
    public struct Pose
    {
        Vector2 _position;
        double _bearing;
		readonly bool _freezed;

		//You can not see empty ctor, but it exists
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
			set
			{
				DC.Contract.Assume(!_freezed);
				_position = value;
			}
        }

        [DataMember]
		public double Bearing
        {
            get { return _bearing; }
	        set
	        {
				DC.Contract.Assume(!_freezed);
		        _bearing = value;
	        }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Position, Bearing);
        }
    }
}