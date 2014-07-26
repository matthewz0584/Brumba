using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
    public class PathPlanner
    {
        private readonly OccupancyGrid _inflatedMap;
        private readonly OccupancyGridPathSearchProblem _occupancyGridPathSearchProblem;
        private readonly AStar<Point> _aStar;

        public PathPlanner(OccupancyGrid map, float robotDiameter)
        {
            DC.Contract.Requires(map != null);
            DC.Contract.Requires(robotDiameter > 0);

            _inflatedMap = new MapInflater(map, robotDiameter).Inflate();
            _occupancyGridPathSearchProblem = new OccupancyGridPathSearchProblem(_inflatedMap, new JumpPointGenerator(_inflatedMap));
            _aStar = new AStar<Point>(_occupancyGridPathSearchProblem);
        }

        public IEnumerable<Vector2> Plan(Vector2 from, Vector2 to)
        {
            _occupancyGridPathSearchProblem.InitialState = _inflatedMap.PosToCell(from);
            _occupancyGridPathSearchProblem.GoalState = _inflatedMap.PosToCell(to);

            var path = _aStar.GraphSearch();
            return path != null ? path.Select(c => _inflatedMap.CellToPos(c)) : null;
        }
    }
}