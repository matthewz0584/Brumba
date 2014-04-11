using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Brumba.DsspUtils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Diagnostics;
using Microsoft.Dss.ServiceModel.Dssp;
using xColor = Microsoft.Xna.Framework.Color;
using xPoint = Microsoft.Xna.Framework.Point;
using DC = System.Diagnostics.Contracts;

namespace Brumba.MapProvider
{
    [Contract(Contract.Identifier)]
    [DisplayName("Brumba Map Provider")]
    [Description("no description provided")]
    public class MapProviderService : DsspServiceExposing
	{
#pragma warning disable 0649
		[ServiceState]
		[InitialStatePartner(Optional = false)] private MapProviderState _state;
#pragma warning restore 0649

		[ServicePort("/MapProvider", AllowMultipleInstances = true)]
        MapProviderOperations _mainPort = new MapProviderOperations();

		public MapProviderService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
            DC.Contract.Requires(creationPort != null);
        }

        protected override void Start()
        {
            DC.Contract.Assume(_state != null);
            DC.Contract.Assume(!string.IsNullOrEmpty(_state.MapImageFile));
            DC.Contract.Assume(!string.IsNullOrEmpty(Path.GetFullPath(_state.MapImageFile))); //Does not throw
            DC.Contract.Assume(_state.GridCellSize > 0);
            DC.Contract.Assume(_state.OccupiedColors != null);
            DC.Contract.Assume(_state.OccupiedColors.Any());

            var mapImageFile = Path.IsPathRooted(_state.MapImageFile)
                ? Path.Combine(LayoutPaths.RootDir, _state.MapImageFile.Trim(Path.DirectorySeparatorChar))
                : Path.Combine(LayoutPaths.RootDir, LayoutPaths.MediaDir, _state.MapImageFile);

            if (!File.Exists(mapImageFile))
            {
                LogError(MapProviderLogCategory.MapImageFileNotFound, mapImageFile);
                Shutdown();
                return;
            }

            _state.Map = CreateOccupancyGrid((Bitmap)Image.FromFile(mapImageFile),
                _state.GridCellSize, _state.OccupiedColors, _state.UnoccupiedColor);

			base.Start();
        }

        public static OccupancyGrid CreateOccupancyGrid(Bitmap bitmap, double gridCellSize, IEnumerable<xColor> occupiedColors, xColor unoccupiedColor)
        {
            DC.Contract.Requires(bitmap != null);
            DC.Contract.Requires(gridCellSize > 0);
            DC.Contract.Requires(occupiedColors != null);
            DC.Contract.Requires(occupiedColors.Any());
            DC.Contract.Ensures(DC.Contract.Result<OccupancyGrid>().CellSize == (float)gridCellSize);
            DC.Contract.Ensures(DC.Contract.Result<OccupancyGrid>().SizeInCells == new xPoint(bitmap.Width, bitmap.Height));

            var occupiedPixels = new PixelColorClassifier().
                Classify(bitmap, occupiedColors.Concat(new[] { unoccupiedColor })).
                Where(p => p.Key != unoccupiedColor).SelectMany(p => p.Value);
            var occupancyGrid = new bool[bitmap.Height, bitmap.Width];
            foreach (var point in occupiedPixels)
                occupancyGrid[point.Y, point.X] = true;
            return new OccupancyGrid(occupancyGrid, (float)gridCellSize);
        }
    }

    [CategoryNamespace("http://brumba.ru/contracts/2014/03/mapproviderservice.html")]
    public enum MapProviderLogCategory
    {
        [OperationalCategory(TraceLevel.Error, LogCategoryFlags.None)]
        [CategoryArgument(0, "MapImageFilePath")]
        MapImageFileNotFound
    }
}