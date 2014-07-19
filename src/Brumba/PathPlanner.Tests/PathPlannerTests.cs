using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
    }

    public class PathPlanner
    {
        public static OccupancyGrid InflateMap(OccupancyGrid map, double delta)
        {
            throw new System.NotImplementedException();
        }
    }
}