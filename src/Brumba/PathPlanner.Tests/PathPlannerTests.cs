using System.Drawing;
using System.Linq;
using Brumba.MapProvider;
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
            var map = MapProviderService.CreateOccupancyGrid((Bitmap)Image.FromFile("simple_house.bmp"),
                0.1, new[] { Color.Black }, Color.White);

            var pp = new PathPlanner(map: map, robotDiameter: 0.2f);

            var checkPoints = pp.Plan(from: new Vector2(0.1f, 3.5f), to: new Vector2(5.5f, 1.5f));

            //Console.Write("{0} ", new Point(map.PosToCell(new Vector2(0.1f, 3.5f)).X, map.SizeInCells.Y - map.PosToCell(new Vector2(0.1f, 3.5f)).Y - 1));
            //foreach (var cp in checkPoints)
            //{
            //    var c = new Point(map.PosToCell(cp).X, map.SizeInCells.Y - map.PosToCell(cp).Y - 1);
            //    Console.Write("{0} ", c);
            //}

            //simple_house_path.bmp
            Assert.That(checkPoints.Select(map.PosToCell), Is.EquivalentTo(new[]
            {
                new Point(10, 43), new Point(21, 43), new Point(22, 42), new Point(23, 41), new Point(28, 36),
                new Point(38, 26), new Point(41, 26), new Point(61, 26), new Point(62, 25), new Point(63, 24),
                new Point(63, 21), new Point(61, 19), new Point(59, 19), new Point(54, 14)
            }));
        }

        [Test]
        public void SearchInGoal()
        {
            //| | |
            //|x| |
            var map = new OccupancyGrid(new[,]
            {
                { false, false },
                { false, false },
            }, 0.2f);

            var pp = new PathPlanner(map: map, robotDiameter: 0.01f);

            var checkPoints = pp.Plan(from: new Vector2(0.1f, 0.1f), to: new Vector2(0.1f, 0.1f));

            Assert.That(checkPoints, Is.Empty);
        }

        [Test]
        public void SearchStraight()
        {
            //| | | |
            //|x| |g|
            //| | | |
            var map = new OccupancyGrid(new[,]
            {
                { false, false, false },
                { false, false, false },
                { false, false, false }
            }, 0.2f);

            var pp = new PathPlanner(map: map, robotDiameter: 0.01f);

            var checkPoints = pp.Plan(from: new Vector2(0.1f, 0.3f), to: new Vector2(0.5f, 0.3f));

            Assert.That(checkPoints, Is.EquivalentTo(new[] { new Vector2(0.5f, 0.3f) }));
        }

        [Test]
        public void SearchStraightIntermediateCheckPoint()
        {
            //| | | |g|
            //| | | | |
            //| |*| | |
            //|x| |O| |
            var map = new OccupancyGrid(new[,]
            {
                { false, false, true, false },
                { false, false, false, false },
                { false, false, false, false },
                { false, false, false, false }
            }, 0.2f);

            var pp = new PathPlanner(map: map, robotDiameter: 0.01f);

            var checkPoints = pp.Plan(from: new Vector2(0.1f, 0.1f), to: new Vector2(0.7f, 0.7f));

            Assert.That(checkPoints, Is.EquivalentTo(new[] { new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f) }));
        }

        [Test]
        public void SearchPolyline()
        {
            //| | |*| |
            //| |*|O|*|
            //| |*|O| |
            //|x| |O|g|
            var map = new OccupancyGrid(new[,]
            {
                { false, false, true, false },
                { false, false, true, false },
                { false, false, true, false },
                { false, false, false, false }
            }, 0.2f);

            var pp = new PathPlanner(map: map, robotDiameter: 0.01f);

            var checkPoints = pp.Plan(from: new Vector2(0.1f, 0.1f), to: new Vector2(0.7f, 0.1f));

            Assert.That(checkPoints, Is.EquivalentTo(new[] { new Vector2(0.3f, 0.3f), new Vector2(0.3f, 0.5f), new Vector2(0.5f, 0.7f), new Vector2(0.7f, 0.5f), new Vector2(0.7f, 0.1f) }));
        }

        [Test]
        public void SearchFailed()
        {
            //| |O| |
            //|x|O|g|
            //| |O| |
            var map = new OccupancyGrid(new[,]
            {
                { false, true, false },
                { false, true, false },
                { false, true, false }
            }, 0.2f);

            var pp = new PathPlanner(map: map, robotDiameter: 0.01f);

            var checkPoints = pp.Plan(from: new Vector2(0.1f, 0.3f), to: new Vector2(0.5f, 0.3f));

            Assert.That(checkPoints, Is.Null);
        }

        [Test]
        public void MapInflation()
        {
            //| |*| |
            //|x|O|g|
            //|O|0|O|
            var map = new OccupancyGrid(new[,]
            {
                { false, true, false },
                { false, false, false },
                { false, false, false }
            }, 0.2f);

            var pp = new PathPlanner(map: map, robotDiameter: 0.1f);

            var checkPoints = pp.Plan(from: new Vector2(0.1f, 0.3f), to: new Vector2(0.5f, 0.3f));

            Assert.That(checkPoints, Is.EquivalentTo(new[] { new Vector2(0.3f, 0.5f), new Vector2(0.5f, 0.3f) }));
        }
    }
}