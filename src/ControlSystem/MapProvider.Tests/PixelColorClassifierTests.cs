using System.Drawing;
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
            Assert.That(classPixels[xColor.Red].All(p => p.X <= 133 && p.X >= 112));
			Assert.That(classPixels[xColor.Red].All(p => p.Y >= (180 - 1 - 131) && p.Y <= (180 - 1 - 125)));

            Assert.That(classPixels[xColor.Blue].Count, Is.EqualTo(7 * 13));
            Assert.That(classPixels[xColor.Blue].All(p => p.X <= 34 && p.X >= 27));
			Assert.That(classPixels[xColor.Blue].All(p => p.Y >= (180 - 1 - 43) && p.Y <= (180 - 1 - 31)));

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
    }
}