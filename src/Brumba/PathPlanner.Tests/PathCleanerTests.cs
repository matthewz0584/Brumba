using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.PathPlanner.Tests
{
    [TestFixture]
    public class PathCleanerTests
    {
        [Test]
        public void StraightLine()
        {
            var pc = new PathCleaner();

            Assert.That(pc.Clean(new List<Point> { new Point(0, 0), new Point(2, 2) }), Is.EquivalentTo(
                new[] { new Point(0, 0), new Point(2, 2) }));

            Assert.That(pc.Clean(new List<Point> { new Point(0, 0), new Point(1, 1), new Point(2, 2), new Point(3, 3) }), Is.EquivalentTo(
                new [] {new Point(0, 0), new Point(3, 3) }));
        }

        [Test]
        public void Polyline()
        {
            var pc = new PathCleaner();

            Assert.That(pc.Clean(new List<Point> { new Point(0, 0), new Point(2, 2), new Point(3, 2) }), Is.EquivalentTo(
                new[] { new Point(0, 0), new Point(2, 2), new Point(3, 2) }));

            Assert.That(pc.Clean(new List<Point> { new Point(0, 0), new Point(1, 1), new Point(2, 2), new Point(3, 2), new Point(4, 2) }), Is.EquivalentTo(
                new[] { new Point(0, 0), new Point(2, 2), new Point(4, 2) }));
        }

        [Test]
        public void Duplicates()
        {
            var pc = new PathCleaner();

            Assert.That(pc.Clean(new List<Point> { new Point(0, 0), new Point(0, 0), new Point(1, 1), new Point(1, 1), new Point(2, 2)}), Is.EquivalentTo(
                new[] { new Point(0, 0), new Point(2, 2) }));
        }

        [Test]
        public void Empty()
        {
            var pc = new PathCleaner();

            Assert.That(pc.Clean(new List<Point>()), Is.Empty);
            Assert.That(pc.Clean(new List<Point> { new Point(0, 0) }), Is.EquivalentTo(new[] { new Point(0, 0) }));
            Assert.That(pc.Clean(new List<Point> { new Point(0, 0), new Point(0, 0) }), Is.EquivalentTo(new[] { new Point(0, 0) }));
        }

        [Test]
        public void WithinSomeMargin()
        {
            var pc = new PathCleaner();

            Assert.That(pc.Clean(new List<Point> { new Point(0, 0), new Point(1000, 1001), new Point(2000, 2000) }), Is.EquivalentTo(
                new[] { new Point(0, 0), new Point(2000, 2000) }));
        }
    }
}