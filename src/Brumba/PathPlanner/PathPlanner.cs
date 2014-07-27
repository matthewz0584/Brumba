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
            DC.Contract.Ensures(InflatedMap != null);
            DC.Contract.Ensures(_occupancyGridPathSearchProblem != null);
            DC.Contract.Ensures(_aStar != null);

            _inflatedMap = new MapInflater(map, robotDiameter).Inflate();
            _occupancyGridPathSearchProblem = new OccupancyGridPathSearchProblem(InflatedMap, new JumpPointGenerator(InflatedMap));
            _aStar = new AStar<Point>(_occupancyGridPathSearchProblem);
        }

        public OccupancyGrid InflatedMap
        {
            get { return _inflatedMap; }
        }

        public IEnumerable<Vector2> Plan(Vector2 from, Vector2 to)
        {
            DC.Contract.Requires(InflatedMap.Covers(from) && !InflatedMap[from]);
            DC.Contract.Requires(InflatedMap.Covers(to) && !InflatedMap[to]);
            DC.Contract.Assume(_occupancyGridPathSearchProblem != null);
            DC.Contract.Assume(_aStar != null);

            _occupancyGridPathSearchProblem.InitialState = InflatedMap.PosToCell(from);
            _occupancyGridPathSearchProblem.GoalState = InflatedMap.PosToCell(to);

            var path = _aStar.GraphSearch();
            return path != null ? path.Select(c => InflatedMap.CellToPos(c)) : null;
        }
    }
}