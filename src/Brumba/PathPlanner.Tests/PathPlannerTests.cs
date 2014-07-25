using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Brumba.DsspUtils;
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
            var map = MapProviderService.CreateOccupancyGrid((Bitmap)Image.FromFile("simple_house.bmp"),
                0.1, new[] { Color.Black }, Color.White);

            var pp = new PathPlanner(map: map, robotDiameter: 0.2f);

            var checkPoints = pp.Plan(from: new Vector2(0.1f, 3.5f), to: new Vector2(6, 2));

            Assert.That(checkPoints, Is.EquivalentTo(new [] {new Vector2(), new Vector2(), new Vector2()}));
        }
    }

    class PathPlanner
    {
        public PathPlanner(OccupancyGrid map, float robotDiameter)
        {
        }

        public IEnumerable<Vector2> Plan(Vector2 from, Vector2 to)
        {
            throw new NotImplementedException();
        }
    }
}