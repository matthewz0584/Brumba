using System.Drawing;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using System.Linq;
using xColor = Microsoft.Xna.Framework.Color;
using xPoint = Microsoft.Xna.Framework.Point;

namespace Brumba.MapProvider.Tests
{
    [TestFixture]
    public class PixelColorClassifierTests
    {
        [Test]
        public void Classify()
        {
            var classPixels = new PixelColorClassifier().Classify((Bitmap) Image.FromFile("red_and_blue_boxes.bmp"),
                new[] {xColor.Red, xColor.Blue, xColor.Gray});

            Assert.That(classPixels.Count, Is.EqualTo(3));

            Assert.That(classPixels[xColor.Red].Count, Is.EqualTo(22 * 7));
            Assert.That(classPixels[xColor.Red].All(p => p.X <= 133 && p.X >= 112 && p.Y <= 131 && p.Y >= 124));

            Assert.That(classPixels[xColor.Blue].Count, Is.EqualTo(7 * 13));
            Assert.That(classPixels[xColor.Blue].All(p => p.X <= 34 && p.X >= 27 && p.Y <= 43 && p.Y >= 31));

            Assert.That(classPixels[xColor.Gray].Count, Is.EqualTo(220 * 180 - (22 * 7 + 7 * 13)));
        }

        [Test]
        public void GetColorClass()
        {
            var pcc = new PixelColorClassifier();

            Assert.That(pcc.GetColorClass(xColor.Blue, new[] { xColor.Red, xColor.Blue, xColor.Gray }), Is.EqualTo(xColor.Blue));
            Assert.That(pcc.GetColorClass(xColor.Gray, new[] { xColor.Red, xColor.Blue, xColor.Gray }), Is.EqualTo(xColor.Gray));
            Assert.That(pcc.GetColorClass(xColor.MediumBlue, new[] { xColor.Red, xColor.Blue, xColor.Gray }), Is.EqualTo(xColor.Blue));
            Assert.That(pcc.GetColorClass(xColor.Lime, new[] { xColor.Red, xColor.Blue, xColor.Gray }), Is.EqualTo(xColor.Gray));

            Assert.That(pcc.GetColorClass(xColor.Green, new[] { new xColor(0, 1, 0), xColor.Lime }), Is.EqualTo(xColor.Lime));
            Assert.That(pcc.GetColorClass(xColor.Green, new[] { xColor.Lime, new xColor(0, 1, 0) }), Is.EqualTo(xColor.Lime));
        }

        [Test]
        public void MapProviderServiceCreateOccupancyGrid()
        {
            var occupancyGrid = MapProviderService.CreateOccupancyGrid((Bitmap) Image.FromFile("red_and_blue_boxes.bmp"),
                0.1, new [] {xColor.Red, xColor.Blue}, xColor.Gray);

            Assert.That(occupancyGrid.SizeInCells, Is.EqualTo(new xPoint(220, 180)));
            Assert.That(occupancyGrid.CellSize, Is.EqualTo(0.1f));

            for (var y = 0; y < 180; ++y)
                for (var x = 0; x < 220; ++x)
                    if ((y >= 31 && y <= 43 && x >= 28 && x <= 34) ||
                        (y >= 125 && y <= 131 && x >= 112 && x <= 133))
                        Assert.That(occupancyGrid[new xPoint(x, y)]);
                    else
                        Assert.That(occupancyGrid[new xPoint(x, y)], Is.False);
        }
    }
}