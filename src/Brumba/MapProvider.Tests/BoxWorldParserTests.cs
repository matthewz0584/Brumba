using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Brumba.Simulation.EnvironmentBuilder;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using NSubstitute;
using NUnit.Framework;
using xColor = Microsoft.Xna.Framework.Color;
using xPoint = Microsoft.Xna.Framework.Point;
using xVector2 = Microsoft.Xna.Framework.Vector2;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using xVector4 = Microsoft.Xna.Framework.Vector4;
using rVector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.MapProvider.Tests
{
    [TestFixture]
    public class BoxWorldParserTests
    {
        private BoxWorldParser _bwp;
        private BoxType _blueType;
        private IPixelBlockGlue _pixelGlue;
        private IPixelColorClassifier _pixelClassifier;
        private BoxWorldParserSettings _settings;

        [SetUp]
        public void Setup()
        {
            _blueType = new BoxType
            {
                ColorOnMapImage = xColor.Blue,
                TextureFileName = "blue box texture",
                Height = 2,
                Mass = 100
            };

            _pixelGlue = Substitute.For<IPixelBlockGlue>();
            _pixelClassifier = Substitute.For<IPixelColorClassifier>();

            _settings = new BoxWorldParserSettings
            {
                GridCellSize = 0.1f,
                FloorType = new ObjectType { ColorOnMapImage = xColor.Gray },
                BoxTypes =
                {
                    new BoxType
                        {
                            ColorOnMapImage = xColor.Red,
                            TextureFileName = "red box texture",
                            Height = 1,
                            Mass = 10
                        },
                    _blueType
                }
            };

            _bwp = new BoxWorldParser(_settings, _pixelGlue, _pixelClassifier);
        }

        [Test]
        public void Acceptance()
        {
            //blue box: (28, 180 - 1 - 43) - (34, 180 - 1 - 31); red box: (112, 131) - (133, 125)
            var entities = new BoxWorldParser(_settings, new PixelBlockGlue(), new PixelColorClassifier()).ParseBoxes((Bitmap)Image.FromFile("red_and_blue_boxes.bmp"));

            Assert.That(entities.Count(), Is.EqualTo(2));
            Assert.That(entities.All(e => e.BoxShape != null));

            var blueBox = entities.Single(e => e.BoxShape.BoxState.TextureFileName == "blue box texture");
			var blueBoxDimensions = (new xVector2(34 + 1, 180 - 1 - 31 + 1) - new xVector2(28, 180 - 1 - 43)) * 0.1f;
			var blueBoxCenter = new xVector2(28, 180 - 1 - 43) * 0.1f + blueBoxDimensions * 0.5f;
            Assert.That(TypeConversion.ToXNA(blueBox.State.Pose.Position).EqualsRelatively(new xVector3(blueBoxCenter.Y, 1f, blueBoxCenter.X), 0.001));
            Assert.That(TypeConversion.ToXNA(blueBox.BoxShape.BoxState.Dimensions), Is.EqualTo(new xVector3(blueBoxDimensions.Y, 2, blueBoxDimensions.X)));
			Assert.That(TypeConversion.ToXNA(blueBox.BoxShape.BoxState.DiffuseColor), Is.EqualTo(new xVector4(0, 0, 1, 1)));
            Assert.That(blueBox.BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(100));
			//Console.WriteLine("blue center {0}", new xVector3(blueBoxCenter.Y, 1f, blueBoxCenter.X));
			//Console.WriteLine("blue dim {0}", new xVector3(blueBoxDimensions.Y, 2, blueBoxDimensions.X));

            var redBox = entities.Single(e => e.BoxShape.BoxState.TextureFileName == "red box texture");
			var redBoxDimensions = (new xVector2(133 + 1, 180 - 1 - 125 + 1) - new xVector2(112, 180 - 1 - 131)) * 0.1f;
            var redBoxCenter = new xVector2(112, 180 - 1 - 131) * 0.1f + redBoxDimensions * 0.5f;
			Assert.That(TypeConversion.ToXNA(redBox.State.Pose.Position), Is.EqualTo(new xVector3(redBoxCenter.Y, 0.5f, redBoxCenter.X)));
            Assert.That(TypeConversion.ToXNA(redBox.BoxShape.BoxState.Dimensions), Is.EqualTo(new xVector3(redBoxDimensions.Y, 1, redBoxDimensions.X)));
			Assert.That(TypeConversion.ToXNA(redBox.BoxShape.BoxState.DiffuseColor), Is.EqualTo(new xVector4(1, 0, 0, 1)));
            Assert.That(redBox.BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(10));
			//Console.WriteLine("red center {0}", new xVector3(redBoxCenter.Y, 1f, redBoxCenter.X));
			//Console.WriteLine("red dim {0}", new xVector3(redBoxDimensions.Y, 0.5f, redBoxDimensions.X));
        }

        [Test]
        public void ParseBoxes()
        {
            var bitmap = new Bitmap(10, 20);

            var colorClassPixels = new Dictionary<xColor, List<xPoint>>
            {
                {xColor.Red, new List<xPoint> {new xPoint(1, 2)}},
                {xColor.Blue, new List<xPoint> {new xPoint(10, 20)}},
                {xColor.Gray, new List<xPoint> {new xPoint(100, 200)}},
            };
            _pixelClassifier.Classify(bitmap, Arg.Any<IEnumerable<xColor>>()).Returns(colorClassPixels);

            _pixelGlue.GluePixelBlocks(colorClassPixels[xColor.Blue]).
                Returns(new List<PixelBlock> { new PixelBlock(new xPoint(1, 2), new xPoint(1, 1)), new PixelBlock(new xPoint(2, 3), new xPoint(1, 2)) });

            _pixelGlue.GluePixelBlocks(colorClassPixels[xColor.Red]).
                Returns(new List<PixelBlock> { new PixelBlock(new xPoint(4, 5), new xPoint(1, 1)) });

            _pixelGlue.GluePixelBlocks(colorClassPixels[xColor.Gray]).
                Returns(new List<PixelBlock> { new PixelBlock(new xPoint(7, 8), new xPoint(1, 1)) });

            var entities = _bwp.ParseBoxes(bitmap).ToList();

            _pixelClassifier.Received().Classify(bitmap, Arg.Is<IEnumerable<xColor>>(
                cs => cs.SequenceEqual(new[] { xColor.Red, xColor.Blue, xColor.Gray })));

            _pixelGlue.Received().GluePixelBlocks(colorClassPixels[xColor.Blue]);
            _pixelGlue.Received().GluePixelBlocks(colorClassPixels[xColor.Red]);
            _pixelGlue.DidNotReceive().GluePixelBlocks(colorClassPixels[xColor.Gray]);

            Assert.That(entities.Count(), Is.EqualTo(3));
            Assert.That(entities.All(e => e.BoxShape != null));

            Assert.That(entities.Count(e => e.BoxShape.BoxState.TextureFileName == "blue box texture"), Is.EqualTo(2));
            Assert.That(entities.Count(e => e.BoxShape.BoxState.TextureFileName == "red box texture"), Is.EqualTo(1));
        }

        [Test]
        public void ParseTypeBoxes()
        {
            var points = new xPoint[1];

            _pixelGlue.GluePixelBlocks(points).Returns(new[] { new PixelBlock(new xPoint(1, 1), new xPoint(2, 3)), new PixelBlock(new xPoint(5, 6), new xPoint(1, 1)) });

            var boxes = _bwp.ParseTypeBoxes(_blueType, points);

            _pixelGlue.Received().GluePixelBlocks(points);

            Assert.That(boxes.Count(), Is.EqualTo(2));

            var boxesOrdered = boxes.OrderByDescending(e => e.BoxShape.BoxState.Dimensions.X);
            Assert.That(boxesOrdered.First().State.Pose.Position, Is.EqualTo(new rVector3(0.25f, (float)_blueType.Height / 2, 0.2f)));
            Assert.That(boxesOrdered.First().BoxShape.BoxState.Dimensions, Is.EqualTo(new rVector3(0.3f, (float)_blueType.Height, 0.2f)));
            Assert.That(boxesOrdered.First().BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(_blueType.Mass));
            Assert.That(boxesOrdered.First().BoxShape.BoxState.TextureFileName, Is.EqualTo(_blueType.TextureFileName));
			Assert.That(TypeConversion.ToXNA(boxesOrdered.First().BoxShape.BoxState.DiffuseColor), Is.EqualTo(new xVector4(0, 0, 1, 1f)));

	        Assert.That(TypeConversion.ToXNA(boxesOrdered.Last().State.Pose.Position).EqualsRelatively(new xVector3(0.65f, (float)_blueType.Height / 2, 0.55f), 0.001));
            Assert.That(boxesOrdered.Last().BoxShape.BoxState.Dimensions, Is.EqualTo(new rVector3(0.1f, (float)_blueType.Height, 0.1f)));
            Assert.That(boxesOrdered.Last().BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(_blueType.Mass));
            Assert.That(boxesOrdered.Last().BoxShape.BoxState.TextureFileName, Is.EqualTo(_blueType.TextureFileName));
			Assert.That(TypeConversion.ToXNA(boxesOrdered.Last().BoxShape.BoxState.DiffuseColor), Is.EqualTo(new xVector4(0, 0, 1, 1f)));
        }

	    [Test]
	    public void MapToSim()
	    {
		    Assert.That(BoxWorldParser.MapToSim(new xVector2(1, 2), 3), Is.EqualTo(new rVector3(2, 3, 1)));
			Assert.That(BoxWorldParser.SimToMap(new rVector3(3, 2, 1)), Is.EqualTo(new xVector2(1, 3)));

			Assert.That(BoxWorldParser.SimToMap(UIMath.EulerToQuaternion(new Vector3(0, 90, 0))), Is.EqualTo(3 * Constants.PiOver2).Within(1e-5));
			Assert.That(BoxWorldParser.SimToMap(UIMath.EulerToQuaternion(new Vector3(0, 45, 0))), Is.EqualTo(5 * Constants.PiOver4).Within(1e-5));
	    }
    }
}