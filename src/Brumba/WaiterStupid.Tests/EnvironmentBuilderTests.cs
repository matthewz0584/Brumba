using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using Mrpm = Microsoft.Robotics.PhysicalModel;
using Color = System.Drawing.Color;

namespace Brumba.WaiterStupid.Tests
{
	[TestFixture]
	public class EnvironmentBuilderTests
	{
		[Test]
		public void CreateEntities()
		{
			var eb = new EnvironmentBuilder(
				new EnvironmentBuilderState
					{
						GridCellSize = 0.1,
						Background = Color.Gray,
						BoxTypes =
						{
							new BoxType
							{
								ColorOnImage = Color.Red,
								TextureFile = "red box texture",
								Height = 1,
								Mass = 10
							},
							new BoxType
							{
								ColorOnImage = Color.Blue,
								TextureFile = "blue box texture",
								Height = 2,
								Mass = 100
							}
						}
					});
			//blue box: (27, 31) - (34, 43); red box: (112, 124) - (133, 131)
			var entities = eb.CreateEntities((Bitmap) Image.FromFile("red_and_blue_boxes.bmp"));

			Assert.That(entities.Count(), Is.EqualTo(2));
			Assert.That(entities.All(e => e.BoxShape != null), Is.EqualTo(2));
			
			var redBox = entities.Single(e => e.BoxShape.BoxState.TextureFileName == "red box texture");
			var redBoxCenterInPixels = new Vector2(27, 31) + (new Vector2(34, 43) - new Vector2(27, 31)) * 0.5f;
			Assert.That(ToXna(redBox.State.Pose.Position), Is.EqualTo(new Vector3(redBoxCenterInPixels * 0.1f, 0.5f)));
			var redBoxDimensionsInPixels = new Vector2(34, 43) - new Vector2(27, 31);
			Assert.That(ToXna(redBox.BoxShape.BoxState.Dimensions), Is.EqualTo(new Vector3(redBoxDimensionsInPixels * 0.1f, 1)));
			Assert.That(redBox.State.MassDensity.Mass, Is.EqualTo(10));

			var blueBox = entities.Single(e => e.BoxShape.BoxState.TextureFileName == "blue box texture");
			var blueBoxCenterInPixels = new Vector2(112, 124) + (new Vector2(133, 131) - new Vector2(112, 124)) * 0.5f;
			Assert.That(ToXna(blueBox.State.Pose.Position), Is.EqualTo(new Vector3(blueBoxCenterInPixels * 0.1f, 1)));
			var blueBoxDimensionsInPixels = new Vector2(133, 131) - new Vector2(112, 124);
			Assert.That(ToXna(blueBox.BoxShape.BoxState.Dimensions), Is.EqualTo(new Vector3(blueBoxDimensionsInPixels * 0.1f, 1)));
			Assert.That(redBox.State.MassDensity.Mass, Is.EqualTo(100));
		}

		[Test]
		[Ignore]
		public void CreateMap()
		{
		}

		static Vector3 ToXna(Mrpm.Vector3 vec)
		{
			return TypeConversion.ToXNA(vec);
		}
	}

	public class EnvironmentBuilder
	{
		public EnvironmentBuilder(EnvironmentBuilderState state)
		{
			throw new System.NotImplementedException();
		}

		public IEnumerable<SingleShapeEntity> CreateEntities(Bitmap bitmap)
		{
			throw new System.NotImplementedException();
		}
	}

	public class EnvironmentBuilderState
	{
		public double GridCellSize { get; set; }
		public Color Background { get; set; }
		public List<BoxType> BoxTypes { get; set; }
	}

	public class BoxType
	{
		public Color ColorOnImage { get; set; }
		public string TextureFile { get; set; }
		public double Height { get; set; }
		public double Mass { get; set; }
	}
}