using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Xna.Framework;

namespace Brumba.PathPlanner
{
    public class PathCleaner
    {
        public IEnumerable<Point> Clean(IEnumerable<Point> path)
        {
            if (!path.Any())
                return path;

            path = path.FilterSequencialDuplicates().ToList();

            if (path.Count() == 1)
                return path;

            var derivative1 = path.Skip(1).Zip(path, 
                (pR, pL) => Vector2.Normalize(new Vector2(pR.X, pR.Y) - new Vector2(pL.X, pL.Y)));
            var derivative2 = derivative1.Skip(1).Zip(derivative1,
                (dR, dL) => dL - dR);
            return  path.First().AsCol().Concat(
                    path.Skip(1).Take(path.Count() - 2).
                        Zip(derivative2, (p, d2) => new { Point = p, IsPlato = (d2.LengthSquared() < 1e-5)}).
                        Where(pp => !pp.IsPlato).Select(pp => pp.Point)).
                    Concat(path.Last().AsCol());
        }
    }
}