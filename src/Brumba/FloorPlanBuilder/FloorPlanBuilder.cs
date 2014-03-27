using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Point = Microsoft.Xna.Framework.Point;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Brumba.FloorPlanBuilder
{
	public interface IEnvironmentBuilder
	{
		EnvironmentBuilderState State { get; set; }
		IEnumerable<SingleShapeEntity> CreateBoxes(Bitmap bitmap);
	}

	public class FloorPlanBuilder : IEnvironmentBuilder
	{
		public FloorPlanBuilder(EnvironmentBuilderState state)
		{
			State = state;
		}

		public EnvironmentBuilderState State { get; set; }

		public IEnumerable<SingleShapeEntity> CreateBoxes(Bitmap bitmap)
		{
			return ClassifyPixels(bitmap)
					.Where(pair => pair.Key is BoxType)
					.SelectMany(pair => CreateTypeBoxes(pair.Key as BoxType, pair.Value, State.GridCellSize));
		}

		public bool[,] CreateOccupancyGrid(Bitmap bitmap)
		{
			var occupancyGrid = new bool[bitmap.Height, bitmap.Width];
			foreach (var point in ClassifyPixels(bitmap).Where(pair => pair.Key != State.FloorType).SelectMany(pair => pair.Value))
				occupancyGrid[point.Y, point.X] = true;
			return occupancyGrid;
		}

		public IEnumerable<SingleShapeEntity> CreateTypeBoxes(BoxType type, IEnumerable<Point> pixels, float gridCellSize)
		{
			return new PixelGlue().GetPixelBlocks(pixels).Select((pixelBlock, i) =>
				{
					var center =
						(new Vector3(pixelBlock.LeftTop.X, 0, pixelBlock.LeftTop.Y) +
						 new Vector3(pixelBlock.Width / 2.0f, (float)type.Height / 2 / gridCellSize, pixelBlock.Height / 2.0f)) * gridCellSize;
					var dimensions =
						new Vector3(pixelBlock.Width, (float) type.Height/gridCellSize, pixelBlock.Height)*gridCellSize;
					return new SingleShapeEntity(
						new BoxShape(new BoxShapeProperties((float) type.Mass, new Pose(), TypeConversion.FromXNA(dimensions)) 
						{TextureFileName = type.TextureFileName}),
						TypeConversion.FromXNA(center))
						{ State = { Name = string.Format("{0} {1}", type.ColorOnImage, i) }
					};
				});
		}

		public IDictionary<ObjectType, List<Point>> ClassifyPixels(Bitmap bitmap)
		{
			var typesPixels = State.BoxTypes.Concat(new[] { State.FloorType }).ToDictionary(ot => ot, ot => new List<Point>());
			for (var i = 0; i < bitmap.Height; i++)
				for (var j = 0; j < bitmap.Width; j++)
					typesPixels[GetPixelType(bitmap.GetPixel(j, i), typesPixels.Keys)].Add(new Point(j, i));
			return typesPixels;
		}

		public ObjectType GetPixelType(Color color, IEnumerable<ObjectType> objectTypes)
		{
			return objectTypes
				.Select(t => new {Type = t, DistToType = (ColorToVector(t.ColorOnImage) - ColorToVector(color)).Length()})
				.OrderBy(td => td.DistToType).ThenBy(td => td.Type.ColorOnImage.ToArgb())
				.First().Type;
		}

		static Vector3 ColorToVector(Color color)
		{
			return new Vector3(color.R, color.G, color.B);
		}
	}

	public class EnvironmentBuilderState
	{
		public float GridCellSize { get; set; }
		public List<BoxType> BoxTypes { get; set; }
		public ObjectType FloorType { get; set; }

		public EnvironmentBuilderState()
		{
			BoxTypes = new List<BoxType>();
		}
	}

	public class ObjectType
	{
		public Color ColorOnImage { get; set; }
	}

	public class BoxType : ObjectType
	{
		public double Height { get; set; }
		public double Mass { get; set; }
		public string TextureFileName { get; set; }
	}
}