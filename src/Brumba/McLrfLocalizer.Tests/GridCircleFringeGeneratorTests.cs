using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.McLrfLocalizer.Tests
{
    [TestFixture]
    public class GridCircleFringeGeneratorTests
    {
        [Test]
        public void GenerateOnMap()
        {
            var map = new OccupancyGrid(new bool[10, 10], 0.1f);
            Assert.That(new GridCircleFringeGenerator().GenerateOnMap(0, new Point(3, 3), map), Is.EquivalentTo(new[] { new Point(3, 3) }));
            Assert.That(new GridCircleFringeGenerator().GenerateOnMap(1, new Point(3, 3), map),
                Is.EquivalentTo(new[]
                {
                    new Point(2, 3), new Point(2, 4), new Point(3, 4), new Point(4, 4),
                    new Point(4, 3), new Point(4, 2), new Point(3, 2), new Point(2, 2)
                }));
        }

        [Test]
        public void GenerateOnMapNearBorder()
        {
            var map = new OccupancyGrid(new bool[4, 5], 0.1f);
            var r0Fringe = new GridCircleFringeGenerator().GenerateOnMap(0, new Point(4, 3), map);
            Assert.That(new HashSet<Point>(r0Fringe).SetEquals(new HashSet<Point>(new[] { new Point(4, 3) })));
            Assert.That(r0Fringe.Count(), Is.EqualTo(1));

            var r1Fringe = new GridCircleFringeGenerator().GenerateOnMap(1, new Point(4, 3), map);
            Assert.That(new HashSet<Point>(r1Fringe).SetEquals(new HashSet<Point>(new[] { new Point(3, 3), new Point(3, 2), new Point(4, 2) })));
            Assert.That(r1Fringe.Count(), Is.EqualTo(3));
        }

        [Test]
        public void Generate()
        {
            Assert.That(new GridCircleFringeGenerator().Generate(0), Is.EquivalentTo(new []{new Point(0, 0)}));
            Assert.That(new GridCircleFringeGenerator().Generate(1),
                Is.EquivalentTo(new[]
                {
                    new Point(-1, 0), new Point(-1, 1), new Point(-1, -1), new Point(0, 1),
                    new Point(0, -1), new Point(1, 0), new Point(1, 1), new Point(1, -1) 
                }));
            Assert.That(new GridCircleFringeGenerator().Generate(2),
                Is.EquivalentTo(new[]
                {
                    new Point(-2, -2), new Point(-2, -1), new Point(-2, 0), new Point(-2, 1), new Point(-2, 2),
                    new Point(-1, 2), new Point(0, 2), new Point(1, 2),
                    new Point(2, 2), new Point(2, 1), new Point(2, 0), new Point(2, -1), new Point(2, -2),
                    new Point(1, -2), new Point(0, -2), new Point(-1, -2)
                }));
            Assert.That(new GridCircleFringeGenerator().Generate(3).Count(), Is.EqualTo(20));
            Assert.That(new GridCircleFringeGenerator().Generate(4).Count(), Is.EqualTo(24));
        }
    }
}