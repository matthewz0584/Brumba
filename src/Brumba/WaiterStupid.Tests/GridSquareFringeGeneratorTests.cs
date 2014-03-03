using System.Collections.Generic;
using System.Linq;
using Brumba.WaiterStupid.McLocalization;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
    [TestFixture]
    public class GridSquareFringeGeneratorTests
    {
        [Test]
        public void Generate()
        {
            Assert.That(new HashSet<Point>(new GridSquareFringeGenerator(new Point(10, 10)).Generate(new Point(3, 3)).Take(9)).SetEquals(
                new HashSet<Point>(new[] { new Point(3, 3), new Point(4, 3), new Point(4, 4), new Point(3, 4), new Point(2, 4), new Point(2, 3), new Point(2, 2), new Point(3, 2), new Point(4, 2) })));
        }

        [Test]
        public void GenerateNearBorder()
        {
            Assert.That(new HashSet<Point>(new GridSquareFringeGenerator(new Point(5, 4)).Generate(new Point(4, 3)).Take(4)).SetEquals(
                new HashSet<Point>(new[] { new Point(3, 3), new Point(4, 3), new Point(3, 2), new Point(4, 2) })));
        }
    }
}