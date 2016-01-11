using System.Drawing;
using NUnit.Framework;
using xColor = Microsoft.Xna.Framework.Color;
using xPoint = Microsoft.Xna.Framework.Point;

namespace Brumba.MapProvider.Tests
{
    [TestFixture]
    public class MapProviderServiceTests
    {
		[Test]
		public void CreateOccupancyGrid()
		{
			var occupancyGrid = MapProviderService.CreateOccupancyGrid((Bitmap)Image.FromFile("red_and_blue_boxes.bmp"),
				0.1, new[] { xColor.Red, xColor.Blue }, xColor.Gray);

			Assert.That(occupancyGrid.SizeInCells, Is.EqualTo(new xPoint(220, 180)));
			Assert.That(occupancyGrid.CellSize, Is.EqualTo(0.1f));

			for (var y = 0; y < 180; ++y)
				for (var x = 0; x < 220; ++x)
					if ((y >= (180 - 1 - 43) && y <= (180 - 1 - 31) && x >= 28 && x <= 34) ||
						(y >= (180 - 1 - 131) && y <= (180 - 1 - 125) && x >= 112 && x <= 133))
						Assert.That(occupancyGrid[x, y]);
					else
						Assert.That(occupancyGrid[x, y], Is.False);
		}
	}
}