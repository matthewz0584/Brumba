using System.Linq;
using Brumba.MapProvider;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.PathPlanner.Tests
{
    [TestFixture]
    public class MapInflaterTests
    {
        [Test]
        public void Inflate()
        {
            //|O|0|0|O| |
            //| |O|O| | |
            //| | | | |O|
            //|O| | |O|0|
            //|0|O| |O|0|
            var occupancy = new[,]
            {
                {true, false, false, false, true},
                {false, false, false, false, true},
                {false, false, false, false, false},
                {false, false, false, false, false},
                {false, true, true, false, false}
            };
            var inflatedOccupancy = new[,]
            {
                {true, true, false, true, true},
                {true, false, false, true, true},
                {false, false, false, false, true},
                {false, true, true, false, false},
                {true, true, true, true, false}
            };

            var mi = new MapInflater(map: new OccupancyGrid(occupancy, 0.1f), delta: 0.06);
            Assert.That(mi.Inflate().All(cv => cv.Item2 == inflatedOccupancy[cv.Item1.Y, cv.Item1.X]));
        }

        [Test]
        public void CellInflation()
        {
            var mi = new MapInflater(map: new OccupancyGrid(new bool[1, 1], 0.1f), delta: 0.06);

            Assert.That(mi.CellInflation(), Is.EquivalentTo(new[] { new Point(0, 0), new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0) }));

            mi = new MapInflater(map: new OccupancyGrid(new bool[1, 1], 0.2f), delta: 0.1);

            Assert.That(mi.CellInflation(), Is.EquivalentTo(new[] { new Point(0, 0), new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0) }));

            mi = new MapInflater(map: new OccupancyGrid(new bool[1, 1], 0.1f), delta: 0.1);
            Assert.That(mi.CellInflation(), Is.EquivalentTo(new[] { new Point(0, 0), new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0), new Point(1, 1), new Point(1, -1), new Point(-1, 1), new Point(-1, -1) }));

            mi = new MapInflater(map: new OccupancyGrid(new bool[1, 1], 0.1f), delta: 0.15);
            Assert.That(mi.CellInflation(),
                Is.EquivalentTo(new[]
                {
                    new Point(0, 0), new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0),
                    new Point(1, 1), new Point(1, -1), new Point(-1, 1), new Point(-1, -1),
                    new Point(-2, 0), new Point(2, 0), new Point(0, 2), new Point(0, -2)
                }));
        }
    }
}