using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using System.Drawing;
using W3C.Soap;

namespace Brumba.MapProvider
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/03/mapproviderservice.html";
    }

    [DataContract]
    public class MapProviderState
    {
		[DataMember]
		public string MapImageFile { get; set; }
		[DataMember]
		public double GridCellSize { get; set; }
        [DataMember]
        public Color UnoccupiedColor { get; set; }
        [DataMember]
        public List<Color> OccupiedColors { get; set; }
        [DataMember]
        public OccupancyGrid Map { get; set; }
    }

    [ServicePort]
    public class MapProviderOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

	public class Get : Get<GetRequestType, PortSet<MapProviderState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

		public Get(GetRequestType body, PortSet<MapProviderState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
}