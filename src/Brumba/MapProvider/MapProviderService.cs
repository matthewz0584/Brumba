using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using Brumba.DsspUtils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using DC = System.Diagnostics.Contracts;

namespace Brumba.MapProvider
{
    [Contract(Contract.Identifier)]
    [DisplayName("Brumba Map Provider")]
    [Description("no description provided")]
    public class MapProviderService : DsspServiceExposing
    {
        [ServiceState]
		[InitialStatePartner(Optional = false)]
        MapProviderState _state;

        [ServicePort("/MapProvider", AllowMultipleInstances = true)]
        MapProviderOperations _mainPort = new MapProviderOperations();

		public MapProviderService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        protected override void Start()
        {
			base.Start();

	        Activate(Arbiter.FromHandler(CreateMap));
        }

        void CreateMap()
        {
            var mapImageFile = Path.IsPathRooted(_state.MapImageFile)
                ? Path.Combine(LayoutPaths.RootDir, _state.MapImageFile.Trim(Path.DirectorySeparatorChar))
                : Path.Combine(LayoutPaths.RootDir, LayoutPaths.MediaDir, _state.MapImageFile);

            _state.Map = CreateOccupancyGrid((Bitmap)Image.FromFile(mapImageFile),
                _state.GridCellSize, _state.OccupiedColors, _state.UnoccupiedColor);
        }

        public static OccupancyGrid CreateOccupancyGrid(Bitmap bitmap, double gridCellSize, IEnumerable<Color> occupiedColors, Color unoccupiedColor)
        {
            var occupiedPixels = new PixelColorClassifier().
                Classify(bitmap, occupiedColors.Concat(new[] { unoccupiedColor })).
                Where(p => p.Key != unoccupiedColor).SelectMany(p => p.Value);
            var occupancyGrid = new bool[bitmap.Height, bitmap.Width];
            foreach (var point in occupiedPixels)
                occupancyGrid[point.Y, point.X] = true;
            return new OccupancyGrid(occupancyGrid, (float)gridCellSize);
        }
    }
}