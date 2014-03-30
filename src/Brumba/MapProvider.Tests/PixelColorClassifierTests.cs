using System.Drawing;
using NUnit.Framework;
using System.Linq;
using Color = System.Drawing.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace Brumba.MapProvider.Tests
{
    [TestFixture]
    public class PixelColorClassifierTests
    {
        [Test]
        public void Classify()
        {
            var classPixels = new PixelColorClassifier().Classify((Bitmap) Image.FromFile("red_and_blue_boxes.bmp"),
                new[] {Color.Red, Color.Blue, Color.Gray});

            Assert.That(classPixels.Count, Is.EqualTo(3));

            Assert.That(classPixels[Color.Red].Count, Is.EqualTo(22 * 7));
            Assert.That(classPixels[Color.Red].All(p => p.X <= 133 && p.X >= 112 && p.Y <= 131 && p.Y >= 124));

            Assert.That(classPixels[Color.Blue].Count, Is.EqualTo(7 * 13));
            Assert.That(classPixels[Color.Blue].All(p => p.X <= 34 && p.X >= 27 && p.Y <= 43 && p.Y >= 31));

            Assert.That(classPixels[Color.Gray].Count, Is.EqualTo(220 * 180 - (22 * 7 + 7 * 13)));
        }

        [Test]
        public void GetColorClass()
        {
            var pcc = new PixelColorClassifier();

            Assert.That(pcc.GetColorClass(Color.Blue, new[] { Color.Red, Color.Blue, Color.Gray }), Is.EqualTo(Color.Blue));
            Assert.That(pcc.GetColorClass(Color.Gray, new[] { Color.Red, Color.Blue, Color.Gray }), Is.EqualTo(Color.Gray));
            Assert.That(pcc.GetColorClass(Color.MediumBlue, new[] { Color.Red, Color.Blue, Color.Gray }), Is.EqualTo(Color.Blue));
            Assert.That(pcc.GetColorClass(Color.Lime, new[] { Color.Red, Color.Blue, Color.Gray }), Is.EqualTo(Color.Gray));

            Assert.That(pcc.GetColorClass(Color.Green, new[] { Color.FromArgb(0, 0, 1, 0), Color.Lime }), Is.EqualTo(Color.Lime));
            Assert.That(pcc.GetColorClass(Color.Green, new[] { Color.Lime, Color.FromArgb(0, 0, 1, 0) }), Is.EqualTo(Color.Lime));
        }

        [Test]
        public void MapProviderServiceCreateOccupancyGrid()
        {
            var occupancyGrid = MapProviderService.CreateOccupancyGrid((Bitmap) Image.FromFile("red_and_blue_boxes.bmp"),
                0.1, new [] {Color.Red, Color.Blue}, Color.Gray);

            Assert.That(occupancyGrid.SizeInCells.X, Is.EqualTo(220));
            Assert.That(occupancyGrid.SizeInCells.Y, Is.EqualTo(180));
            Assert.That(occupancyGrid.CellSize, Is.EqualTo(0.1f));

            for (var y = 0; y < 180; ++y)
                for (var x = 0; x < 220; ++x)
                    if ((y >= 31 && y <= 43 && x >= 28 && x <= 34) ||
                        (y >= 125 && y <= 131 && x >= 112 && x <= 133))
                        Assert.That(occupancyGrid[new Point(x, y)]);
                    else
                        Assert.That(occupancyGrid[new Point(x, y)], Is.False);
        }
    }
}