using System.Collections.Generic;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;

namespace Brumba.Simulation.EnvironmentBuilder
{
	[DataContract]
	public class BoxWorldParserSettings
	{
		[DataMember]
		public float GridCellSize { get; set; }
		[DataMember]
		public List<BoxType> BoxTypes { get; set; }
		[DataMember]
		public ObjectType FloorType { get; set; }

		public BoxWorldParserSettings()
		{
			BoxTypes = new List<BoxType>();
		}
	}

    [DataContract]
    public class ObjectType
    {
        [DataMember]
        public Color ColorOnMapImage { get; set; }
    }

    [DataContract]
    public class BoxType : ObjectType
    {
        [DataMember]
        public double Height { get; set; }
        [DataMember]
        public double Mass { get; set; }
        [DataMember]
        public string TextureFileName { get; set; }
    }
}