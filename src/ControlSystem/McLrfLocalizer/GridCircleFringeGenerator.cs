using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using DC = System.Diagnostics.Contracts;
using Brumba.Utils;
using Microsoft.Xna.Framework;

namespace Brumba.McLrfLocalizer
{
    public class GridCircleFringeGenerator
    {
        public IEnumerable<Point> GenerateOnMap(int radius, Point center, OccupancyGrid map)
        {
            DC.Contract.Requires(radius >= 0);
            DC.Contract.Requires(map != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Point>>().All(map.Covers));

            return Generate(radius).Select(p => p.Plus(center)).Where(map.Covers);
        }

        public IEnumerable<Point> Generate(int radius)
        {
            DC.Contract.Requires(radius >= 0);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Point>>().All(p => p.LengthSq() >= radius * radius));
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Point>>().All(p => p.LengthSq() < (radius + 1) * (radius + 1)));

            for (var x = -radius; x <= radius; ++x)
            {
                for (var y = (int)Math.Ceiling(CircleY(radius, x)); y < CircleY(radius + 1, x); ++y)
                    yield return new Point(x, y);

                for (var y = -(int)Math.Ceiling(CircleY(radius, x)); y > -CircleY(radius + 1, x); --y)
                {
                    if (y == 0) continue;
                    yield return new Point(x, y);
                }
            }
        }

        double CircleY(int radius, int x)
        {
            DC.Contract.Requires(radius >= 0);

            return Math.Sqrt(radius * radius - x * x);
        }
    }
}