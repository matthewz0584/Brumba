using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;

namespace Brumba.Simulation.SimulatedLrf
{
	[DataContract]
	public class LaserRangeFinderExEntity : LaserRangeFinderEntity
	{
		[DataMember]
		public RaycastProperties RaycastProperties_FORDB
		{
			get { return RaycastProperties; }
			set { RaycastProperties = value; }
		}
	}
}
