using System.Collections.Generic;
using System.Drawing;
using Microsoft.Dss.Core.Attributes;

namespace Brumba.Simulation.EnvironmentBuilder
{
	[DataContract]
	public class BoxWorldBuilderSettings
	{
		[DataMember]
		public float GridCellSize { get; set; }
		[DataMember]
		public List<BoxType> BoxTypes { get; set; }
		[DataMember]
		public ObjectType FloorType { get; set; }

		public BoxWorldBuilderSettings()
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