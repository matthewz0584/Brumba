using System;
using System.Collections.Generic;
using DC = System.Diagnostics.Contracts;
using Brumba.Utils;
using Microsoft.Xna.Framework;

namespace Brumba.McLrfLocalizer
{
    public class GridCircleFringeGenerator
    {
        private readonly Point _gridSize;

        public GridCircleFringeGenerator(Point gridSize)
        {
            DC.Contract.Requires(gridSize.X > 0);
            DC.Contract.Requires(gridSize.Y > 0);

            _gridSize = gridSize;
        }

        public Point GridSize
        {
            get { return _gridSize; }
        }

        public IEnumerable<Point> Generate(Point center, int radius)
        {
            DC.Contract.Requires(radius >= 0);

            for (var x = -radius; x <= radius; ++x)
            {
                for (var y = (int)Math.Ceiling(CircleY(radius, x)); y < CircleY(radius + 1, x); ++y)
                {
                    var candidate = new Point(x + center.X, y + center.Y);
                    if (!candidate.Between(new Point(), GridSize))
                        continue;
                    yield return candidate;
                }

                for (var y = -(int)Math.Ceiling(CircleY(radius, x)); y > -CircleY(radius + 1, x); --y)
                {
                    if (y == 0) continue;
                    var candidate = new Point(x + center.X, y + center.Y);
                    if (!candidate.Between(new Point(), GridSize))
                        continue;
                    yield return candidate;
                }
            }
        }

        public IEnumerable<Point> Generate(int radius)
        {
            DC.Contract.Requires(radius >= 0);

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

        static double CircleY(int radius, int x)
        {
            DC.Contract.Requires(radius >= 0);

            return Math.Sqrt(radius * radius - x * x);
        }
    }
}