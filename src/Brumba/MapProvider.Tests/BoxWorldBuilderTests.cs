using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Brumba.Simulation.EnvironmentBuilder;
using Microsoft.Robotics.Simulation.Engine;
using NSubstitute;
using NUnit.Framework;
using xPoint = Microsoft.Xna.Framework.Point;
using xVector2 = Microsoft.Xna.Framework.Vector2;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using rVector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.MapProvider.Tests
{
    [TestFixture]
    public class BoxWorldBuilderTests
    {
        private BoxWorldBuilder _bwb;
        private BoxType _blueType;
        private IPixelBlockGlue _pixelGlue;
        private IPixelColorClassifier _pixelClassifier;
        private BoxWorldBuilderSettings _settings;

        [SetUp]
        public void Setup()
        {
            _blueType = new BoxType
            {
                ColorOnMapImage = Color.Blue,
                TextureFileName = "blue box texture",
                Height = 2,
                Mass = 100
            };

            _pixelGlue = Substitute.For<IPixelBlockGlue>();
            _pixelClassifier = Substitute.For<IPixelColorClassifier>();

            _settings = new BoxWorldBuilderSettings
            {
                GridCellSize = 0.1f,
                FloorType = new ObjectType { ColorOnMapImage = Color.Gray },
                BoxTypes =
                {
                    new BoxType
                        {
                            ColorOnMapImage = Color.Red,
                            TextureFileName = "red box texture",
                            Height = 1,
                            Mass = 10
                        },
                    _blueType
                }
            };

            _bwb = new BoxWorldBuilder(_settings, _pixelGlue, _pixelClassifier);
        }

        [Test]
        public void Acceptance()
        {
            //blue box: (28, 31) - (35, 44); red box: (112, 125) - (134, 132)
            var entities = new BoxWorldBuilder(_settings, new PixelBlockGlue(), new PixelColorClassifier()).CreateBoxes((Bitmap)Image.FromFile("red_and_blue_boxes.bmp"));

            Assert.That(entities.Count(), Is.EqualTo(2));
            Assert.That(entities.All(e => e.BoxShape != null));

            var blueBox = entities.Single(e => e.BoxShape.BoxState.TextureFileName == "blue box texture");
            var blueBoxCenter = (new xVector2(28, 31) + (new xVector2(35, 44) - new xVector2(28, 31)) * 0.5f) * 0.1f;
            Assert.That(blueBox.State.Pose.Position, Is.EqualTo(TypeConversion.FromXNA(new xVector3(blueBoxCenter.X, 1f, blueBoxCenter.Y))));
            var blueBoxDimensions = (new xVector2(35, 44) - new xVector2(28, 31)) * 0.1f;
            Assert.That(blueBox.BoxShape.BoxState.Dimensions, Is.EqualTo(TypeConversion.FromXNA(new xVector3(blueBoxDimensions.X, 2, blueBoxDimensions.Y))));
            Assert.That(blueBox.BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(100));

            var redBox = entities.Single(e => e.BoxShape.BoxState.TextureFileName == "red box texture");
            var redBoxCenter = (new xVector2(112, 125) + (new xVector2(134, 132) - new xVector2(112, 125)) * 0.5f) * 0.1f;
            Assert.That(redBox.State.Pose.Position, Is.EqualTo(TypeConversion.FromXNA(new xVector3(redBoxCenter.X, 0.5f, redBoxCenter.Y))));
            var redBoxDimensions = (new xVector2(134, 132) - new xVector2(112, 125)) * 0.1f;
            Assert.That(redBox.BoxShape.BoxState.Dimensions, Is.EqualTo(TypeConversion.FromXNA(new xVector3(redBoxDimensions.X, 1, redBoxDimensions.Y))));
            Assert.That(redBox.BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(10));
        }

        [Test]
        public void CreateBoxes()
        {
            var bitmap = new Bitmap(10, 20);

            var colorClassPixels = new Dictionary<Color, List<xPoint>>
            {
                {Color.Red, new List<xPoint> {new xPoint(1, 2)}},
                {Color.Blue, new List<xPoint> {new xPoint(10, 20)}},
                {Color.Gray, new List<xPoint> {new xPoint(100, 200)}},
            };
            _pixelClassifier.Classify(bitmap, Arg.Any<IEnumerable<Color>>()).Returns(colorClassPixels);

            _pixelGlue.GluePixelBlocks(colorClassPixels[Color.Blue]).
                Returns(new List<PixelBlock> { new PixelBlock(new xPoint(1, 2), 1, 1), new PixelBlock(new xPoint(2, 3), 1, 2) });

            _pixelGlue.GluePixelBlocks(colorClassPixels[Color.Red]).
                Returns(new List<PixelBlock> { new PixelBlock(new xPoint(4, 5), 1, 1) });

            _pixelGlue.GluePixelBlocks(colorClassPixels[Color.Gray]).
                Returns(new List<PixelBlock> { new PixelBlock(new xPoint(7, 8), 1, 1) });

            var entities = _bwb.CreateBoxes(bitmap).ToList();

            _pixelClassifier.Received().Classify(bitmap, Arg.Is<IEnumerable<Color>>(
                cs => cs.SequenceEqual(new[] { Color.Red, Color.Blue, Color.Gray })));

            _pixelGlue.Received().GluePixelBlocks(colorClassPixels[Color.Blue]);
            _pixelGlue.Received().GluePixelBlocks(colorClassPixels[Color.Red]);
            _pixelGlue.DidNotReceive().GluePixelBlocks(colorClassPixels[Color.Gray]);

            Assert.That(entities.Count(), Is.EqualTo(3));
            Assert.That(entities.All(e => e.BoxShape != null));

            Assert.That(entities.Count(e => e.BoxShape.BoxState.TextureFileName == "blue box texture"), Is.EqualTo(2));
            Assert.That(entities.Count(e => e.BoxShape.BoxState.TextureFileName == "red box texture"), Is.EqualTo(1));
        }

        [Test]
        public void CreateTypeBoxes()
        {
            var points = new xPoint[1];

            _pixelGlue.GluePixelBlocks(points).Returns(new[] { new PixelBlock(new xPoint(1, 1), 2, 3), new PixelBlock(new xPoint(5, 5), 1, 1) });

            var boxes = _bwb.CreateTypeBoxes(_blueType, points);

            _pixelGlue.Received().GluePixelBlocks(points);

            Assert.That(boxes.Count(), Is.EqualTo(2));

            var boxesOrdered = boxes.OrderByDescending(e => e.BoxShape.BoxState.Dimensions.X);
            Assert.That(boxesOrdered.First().State.Pose.Position, Is.EqualTo(new rVector3(0.2f, (float)_blueType.Height / 2, 0.25f)));
            Assert.That(boxesOrdered.First().BoxShape.BoxState.Dimensions, Is.EqualTo(new rVector3(0.2f, (float)_blueType.Height, 0.3f)));
            Assert.That(boxesOrdered.First().BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(_blueType.Mass));
            Assert.That(boxesOrdered.First().BoxShape.BoxState.TextureFileName, Is.EqualTo(_blueType.TextureFileName));

            Assert.That(boxesOrdered.Last().State.Pose.Position, Is.EqualTo(new rVector3(0.55f, (float)_blueType.Height / 2, 0.55f)));
            Assert.That(boxesOrdered.Last().BoxShape.BoxState.Dimensions, Is.EqualTo(new rVector3(0.1f, (float)_blueType.Height, 0.1f)));
            Assert.That(boxesOrdered.Last().BoxShape.BoxState.MassDensity.Mass, Is.EqualTo(_blueType.Mass));
            Assert.That(boxesOrdered.Last().BoxShape.BoxState.TextureFileName, Is.EqualTo(_blueType.TextureFileName));
        }
    }
}