using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.McLrfLocalizer.Tests
{
    [TestFixture]
    public class GridCircleFringeGeneratorTests
    {
        [Test]
        public void Generate()
        {
            var r0Fringe = new GridCircleFringeGenerator(new Point(10, 10)).Generate(new Point(3, 3), 0);
            Assert.That(new HashSet<Point>(r0Fringe).SetEquals(new HashSet<Point>(new[] { new Point(3, 3) })));
            Assert.That(r0Fringe.Count(), Is.EqualTo(1));

            var r1Fringe = new GridCircleFringeGenerator(new Point(10, 10)).Generate(new Point(3, 3), 1);
            Assert.That(new HashSet<Point>(r1Fringe).SetEquals(
                new HashSet<Point>(new[] { new Point(2, 3), new Point(2, 4), new Point(3, 4), new Point(4, 4), new Point(4, 3), new Point(4, 2), new Point(3, 2), new Point(2, 2) })));
            Assert.That(r1Fringe.Count(), Is.EqualTo(8));

            var r2Fringe = new GridCircleFringeGenerator(new Point(10, 10)).Generate(new Point(3, 3), 2);
            Assert.That(new HashSet<Point>(r2Fringe).SetEquals(
                new HashSet<Point>(new[] { new Point(1, 1), new Point(1, 2), new Point(1, 3), new Point(1, 4), new Point(1, 5),
                                           new Point(2, 5), new Point(3, 5), new Point(4, 5),
                                           new Point(5, 5), new Point(5, 4), new Point(5, 3), new Point(5, 2), new Point(5, 1),
                                           new Point(4, 1), new Point(3, 1), new Point(2, 1) })));
            Assert.That(r2Fringe.Count(), Is.EqualTo(16));

            Assert.That(new GridCircleFringeGenerator(new Point(10, 10)).Generate(new Point(4, 4), 3).Count(), Is.EqualTo(20));
            Assert.That(new GridCircleFringeGenerator(new Point(10, 10)).Generate(new Point(5, 5), 4).Count(), Is.EqualTo(24));
        }

        [Test]
        public void GenerateNearBorder()
        {
            var r0Fringe = new GridCircleFringeGenerator(new Point(5, 4)).Generate(new Point(4, 3), 0);
            Assert.That(new HashSet<Point>(r0Fringe).SetEquals(new HashSet<Point>(new[] { new Point(4, 3) })));
            Assert.That(r0Fringe.Count(), Is.EqualTo(1));

            var r1Fringe = new GridCircleFringeGenerator(new Point(5, 4)).Generate(new Point(4, 3), 1);
            Assert.That(new HashSet<Point>(r1Fringe).SetEquals(new HashSet<Point>(new[] { new Point(3, 3), new Point(3, 2), new Point(4, 2) })));
            Assert.That(r1Fringe.Count(), Is.EqualTo(3));
        }
    }
}