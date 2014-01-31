using System.Drawing;
using System.Linq;
using Brumba.FloorPlanBuilder;
using Microsoft.Robotics.Simulation.Engine;
using NUnit.Framework;
using Color = System.Drawing.Color;
using Point = Microsoft.Xna.Framework.Point;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Brumba.FloorPlanBuilderTests
{
	[TestFixture]
	public class FloorPlanBuilderTests
	{
		private FloorPlanBuilder.FloorPlanBuilder _eb;
		private ObjectType _floorType;
		private BoxType _blueType;
		private BoxType _redType;

		[SetUp]
		public void Setup()
		{
			_redType = new BoxType
			{
				ColorOnImage = Color.Red,
				TextureFileName = "red box texture",
				Height = 1,
				Mass = 10
			};
			_blueType = new BoxType
			{
				ColorOnImage = Color.Blue,
				TextureFileName = "blue box texture",
				Height = 2,
				Mass = 100
			};
			_floorType = new ObjectType
			{
				ColorOnImage = Color.Gray,
			};
			_eb = new FloorPlanBuilder.FloorPlanBuilder(new EnvironmentBuilderState
			{
				GridCellSize = 0.1f,
				FloorType = _floorType,
				BoxTypes = { _redType, _blueType }
			});
		}

		[Test]
		public void CreateBoxes()
		{
			//blue box: (28, 31) - (35, 44); red box: (112, 125) - (134, 132)
			var entities = _eb.CreateBoxes((Bitmap)Image.FromFile("red_and_blue_boxes.bmp"));

			Assert.That(entities.Count(), Is.EqualTo(2));
			Assert.That(entities.All(e => e.BoxShape != null));

			var blueBox = entities.Single(e => e.BoxShape.BoxState.TextureFileName == "blue box texture");
			var blueBoxCenter = (new Vector2(28, 31) + (new Vector2(35, 44) - new Vector2(28, 31)) * 0.5f) * 0.1f;
			Assert.That(ToXna(blueBox.State.Pose.Position), Is.EqualTo(new Vector3(blueBoxCenter.X, 1f, blueBoxCenter.Y)));
			var blueBoxDimensions = (new Vector2(35, 44) - new Vector2(28, 31)) * 0.1f;
			Assert.That(ToXna(blueBox.BoxShape.BoxState.Dimensions), Is.EqualTo(new Vector3(blueBoxDimensions.X, 2, blueBoxDimensions.Y)));
			Assert.That(blueBox.BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(100));

			var redBox = entities.Single(e => e.BoxShape.BoxState.TextureFileName == "red box texture");
			var redBoxCenter = (new Vector2(112, 125) + (new Vector2(134, 132) - new Vector2(112, 125)) * 0.5f) * 0.1f;
			Assert.That(ToXna(redBox.State.Pose.Position), Is.EqualTo(new Vector3(redBoxCenter.X, 0.5f, redBoxCenter.Y)));
			var redBoxDimensions = (new Vector2(134, 132) - new Vector2(112, 125)) * 0.1f;
			Assert.That(ToXna(redBox.BoxShape.BoxState.Dimensions), Is.EqualTo(new Vector3(redBoxDimensions.X, 1, redBoxDimensions.Y)));
			Assert.That(redBox.BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(10));
		}

		[Test]
		public void ClassifyPixels()
		{
			var typesPixels = _eb.ClassifyPixels((Bitmap)Image.FromFile("red_and_blue_boxes.bmp"));

			Assert.That(typesPixels.Count(), Is.EqualTo(3));

			Assert.That(typesPixels[_redType].Count(), Is.EqualTo(22 * 7));
			Assert.That(typesPixels[_redType].All(p => p.X <= 133 && p.X >= 112 && p.Y <= 131 && p.Y >= 124));

			Assert.That(typesPixels[_blueType].Count(), Is.EqualTo(7 * 13));
			Assert.That(typesPixels[_blueType].All(p => p.X <= 34 && p.X >= 27 && p.Y <= 43 && p.Y >= 31));

			Assert.That(typesPixels[_floorType].Count(), Is.EqualTo(220 * 180 - (22 * 7 + 7 * 13)));
		}

		[Test]
		public void GetPixelType()
		{
			Assert.That(_eb.GetPixelType(Color.Blue, new[] { _blueType, _redType, _floorType }), Is.EqualTo(_blueType));
			Assert.That(_eb.GetPixelType(Color.Gray, new[] { _blueType, _redType, _floorType }), Is.EqualTo(_floorType));
			Assert.That(_eb.GetPixelType(Color.MediumBlue, new[] { _blueType, _redType, _floorType }), Is.EqualTo(_blueType));
			Assert.That(_eb.GetPixelType(Color.Lime, new[] { _blueType, _redType, _floorType }), Is.EqualTo(_floorType));

			var limeType = new BoxType
			{
				ColorOnImage = Color.Lime,
				TextureFileName = "blue box texture",
				Height = 2,
				Mass = 100
			};
			var blackType = new BoxType
			{
				ColorOnImage = Color.FromArgb(0, 0, 1, 0),
				TextureFileName = "red box texture",
				Height = 1,
				Mass = 10
			};
			Assert.That(_eb.GetPixelType(Color.Green, new[] { blackType, limeType }), Is.EqualTo(limeType));
			Assert.That(_eb.GetPixelType(Color.Green, new[] { limeType, blackType }), Is.EqualTo(limeType));
		}

		[Test]
		public void CreateTypeBoxes()
		{
			var simpleBox = new[]
			{
				new Point(1, 1), new Point(2, 1),
				new Point(1, 2), new Point(2, 2)
			};

			var boxes = _eb.CreateTypeBoxes(_blueType, simpleBox, 0.1f);

			Assert.That(boxes.Count(), Is.EqualTo(1));

			Assert.That(ToXna(boxes.Single().State.Pose.Position), Is.EqualTo(new Vector3(0.2f, (float)_blueType.Height / 2, 0.2f)));
			Assert.That(ToXna(boxes.Single().BoxShape.BoxState.Dimensions), Is.EqualTo(new Vector3(0.2f, (float)_blueType.Height, 0.2f)));
			Assert.That(boxes.Single().BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(_blueType.Mass));
			Assert.That(boxes.Single().BoxShape.BoxState.TextureFileName, Is.EqualTo(_blueType.TextureFileName));
		}

		[Test]
		[Ignore]
		public void CreateMap()
		{
		}

		static Vector3 ToXna(Microsoft.Robotics.PhysicalModel.Vector3 vec)
		{
			return TypeConversion.ToXNA(vec);
		}
	}
}