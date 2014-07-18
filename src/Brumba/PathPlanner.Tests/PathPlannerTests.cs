using System.Collections.Generic;
using System.Drawing;
using Brumba.MapProvider;
using Brumba.McLrfLocalizer;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace Brumba.PathPlanner.Tests
{
    [TestFixture]
    public class PathPlannerTests
    {
        [Test]
        public void Acceptance()
        {
            //var map = MapProviderService.CreateOccupancyGrid((Bitmap)Image.FromFile("simple_house.bmp"),
                //0.1, new[] { Color.Black }, Color.White);

            //var pp = new PathPlanner(map);
        }

        //Revert SearchProblem from ICellExpander interface, rvert its setting from GridPathSp ctor
        [Test]
        public void InflateMap()
        {
            //| | | | | |
            //| |O|O|O| |
            //| |O|0|O| |
            //| |O|O|O| |
            //| | | | | |
            var map = new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false},
                        {false, false, false, false, false}
					}, 0.2f);

            var infMap = PathPlanner.InflateMap(map: map, delta: 0.1);

            Assert.That(AreaOccupied(infMap, new Point(1, 1), new Point(4, 4), true));
            Assert.That(AreaOccupied(infMap, new Point(0, 0), new Point(5, 1), false));
            Assert.That(AreaOccupied(infMap, new Point(0, 4), new Point(5, 5), false));
            Assert.That(AreaOccupied(infMap, new Point(0, 1), new Point(1, 4), false));
            Assert.That(AreaOccupied(infMap, new Point(4, 1), new Point(5, 4), false));
        }

        [Test]
        public void CellInflation()
        {
            var mi = new MapInflater(map: new OccupancyGrid(new bool[1,1], 0.1f), delta: 0.05);

            Assert.That(mi.CellInflation, Is.EquivalentTo(new[] { new Point(0, 0), new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0), new Point(1, 1), new Point(1, -1), new Point(-1, 1), new Point(-1, -1) }));
        }

        bool AreaOccupied(OccupancyGrid map, Point from, Point till, bool occupied)
        {
            for (int x = from.X; x < till.X; ++x)
                for (int y = from.Y; y < till.Y; ++y)
                    if (map[new Point(x, y)] != occupied)
                        return false;
            return true;
        }
    }

    public class MapInflater
    {
        public MapInflater(OccupancyGrid map, double delta)
        {
            var cellInflation = new List<Point>();
            var radius = 0;
            var stop = false;
            while (!stop)
            {
                stop = true;
                foreach (var cell in new GridCircleFringeGenerator(new Point(1, 1)).Generate(radius++))
                {
                    if (map.CellToPos(cell).Length() < map.CellSize*0.6 + delta)
                    {
                        stop = false;
                        cellInflation.Add(cell);
                    }
                }
            }
            CellInflation = cellInflation;
        }

        public IEnumerable<Point> CellInflation { get; private set; }
    }

    public class PathPlanner
    {
        public static OccupancyGrid InflateMap(OccupancyGrid map, double delta)
        {
            throw new System.NotImplementedException();
        }
    }
}