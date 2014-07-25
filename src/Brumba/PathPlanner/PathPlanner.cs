using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Microsoft.Xna.Framework;

namespace Brumba.PathPlanner
{
    public class PathPlanner
    {
        private readonly OccupancyGrid _inflatedMap;
        private readonly OccupancyGridPathSearchProblem _searchProblem;
        private readonly AStar<Point> _aStar;

        public PathPlanner(OccupancyGrid map, float robotDiameter)
        {
            _inflatedMap = new MapInflater(map, robotDiameter).Inflate();
            _searchProblem = new OccupancyGridPathSearchProblem(_inflatedMap);
            _searchProblem.CellExpander = new JumpPointGenerator(_searchProblem);
            _aStar = new AStar<Point>(_searchProblem);
        }

        public IEnumerable<Vector2> Plan(Vector2 from, Vector2 to)
        {
            _searchProblem.InitialState = _inflatedMap.PosToCell(from);
            _searchProblem.GoalState = _inflatedMap.PosToCell(to);
            
            var path = _aStar.GraphSearch();
            return path != null ? path.Select(c => _inflatedMap.CellToPos(c)) : null;
        }
    }
}