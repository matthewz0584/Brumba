using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.WaiterStupid.McLocalization;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
	[TestFixture]
	public class OccupancyGridTests
	{
		[Test]
		public void DistanceToObstacle()
		{
			var m = new OccupancyGrid(
				occupancy: new [,]
				{
					{false, true, false},
					{false, false, false}
				},
				cellSize: 0.2f);

			Assert.That(m.DistanceToObstacle(new Vector2(0.3f, 0.1f)), Is.EqualTo(0));
			Assert.That(m.DistanceToObstacle(new Vector2(0.1f, 0.1f)), Is.EqualTo(0.2).Within(1e-5));
			Assert.That(m.DistanceToObstacle(new Vector2(0.3f, 0.3f)), Is.EqualTo(0.2).Within(1e-5));
			Assert.That(m.DistanceToObstacle(new Vector2(0.1f, 0.3f)), Is.EqualTo(Math.Sqrt(2 * 0.2 * 0.2)).Within(1e-5));

			Assert.That(m.DistanceToObstacle(new Vector2(0.12f, 0.1f)), Is.EqualTo(0.18).Within(1e-5));
		}

		[Test]
		public void SquareFringeGenerator()
		{
			Assert.That(new HashSet<Point>(new SquareFringeGenerator(new Point(10, 10)).Generate(new Point(3, 3)).Take(9)).SetEquals(
				new HashSet<Point>(new [] {	new Point(3, 3), new Point(4, 3), new Point(4, 4), new Point(3, 4), new Point(2, 4), new Point(2, 3), new Point(2, 2), new Point(3, 2), new Point(4, 2) })));
		}

		[Test]
		public void SquareFringeGeneratorNearBorder()
		{
			Assert.That(new HashSet<Point>(new SquareFringeGenerator(new Point(5, 4)).Generate(new Point(4, 3)).Take(4)).SetEquals(
				new HashSet<Point>(new[] { new Point(3, 3), new Point(4, 3), new Point(3, 2), new Point(4, 2) })));
		}
	}
}