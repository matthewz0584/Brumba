using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Brumba.Utils;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class GridCircleFringeGenerator
    {
        private readonly Point _gridSize;

        public GridCircleFringeGenerator(Point gridSize)
        {
            Contract.Requires(gridSize.X > 0);
            Contract.Requires(gridSize.Y > 0);

            _gridSize = gridSize;
        }

        public Point GridSize
        {
            get { return _gridSize; }
        }

        public IEnumerable<Point> Generate(Point center, int radius)
        {
            Contract.Requires(radius >= 0);
            Contract.Requires(center.Between(new Point(), GridSize));

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

        static double CircleY(int radius, int x)
        {
            Contract.Requires(radius >= 0);

            return Math.Sqrt(radius * radius - x * x);
        }
    }
}