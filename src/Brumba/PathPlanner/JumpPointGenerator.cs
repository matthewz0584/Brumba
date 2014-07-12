using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Brumba.Utils;
using Microsoft.Xna.Framework;

namespace Brumba.PathPlanner
{
	public class JumpPointGenerator
	{
		static readonly List<Point> _directions = new List<Point>
		{
			new Point(1, 0),
			new Point(1, 1),
			new Point(0, 1),
			new Point(-1, 1),
			new Point(-1, 0),
			new Point(-1, -1),
			new Point(0, -1),
			new Point(1, -1)
		};

		readonly OccupancyGrid _map;
		readonly ISearchProblem<Point> _searchProblem;

		public JumpPointGenerator(OccupancyGrid map, ISearchProblem<Point> searchProblem)
		{
			_map = map;
			_searchProblem = searchProblem;
		}

		public IEnumerable<Point> Generate(Point from)
		{
			return _directions.Select(dir => JumpToward(from, dir)).Where(jp => jp.HasValue).Select(jp => jp.Value);
		}

		Point? JumpToward(Point from, Point direction)
		{
			var nextCell = from.Plus(direction);
			if (!_map.Covers(nextCell) || _map[nextCell])
				return null;
			if ((nextCell == _searchProblem.GoalState) ||
			    HasForcedNeighbours(nextCell, direction) ||
			    (IsDiagonal(direction) &&
			        (JumpToward(nextCell, new Point(direction.X, 0)).HasValue ||
			        JumpToward(nextCell, new Point(0, direction.Y)).HasValue)))
			    return nextCell;
		    return JumpToward(nextCell, direction);
		}

		bool HasForcedNeighbours(Point cell, Point direction)
		{
		    return IsDiagonal(direction) ?
                HasForcedDiagonalNeighbour(cell, direction, horizontal: true) ||
                HasForcedDiagonalNeighbour(cell, direction, horizontal: false)
                :
                HasForcedStraightNeighbour(cell, direction, up: true) ||
                HasForcedStraightNeighbour(cell, direction, up: false);
		}

        bool HasForcedStraightNeighbour(Point cell, Point direction, bool up)
        {
            var obstacleCell = cell.Plus(direction.Perpendicular().Scale(up ? 1 : -1));
            return _map.Covers(obstacleCell) && _map[obstacleCell] &&
                   _map.Covers(obstacleCell.Plus(direction)) && !_map[obstacleCell.Plus(direction)];
        }

        bool HasForcedDiagonalNeighbour(Point cell, Point direction, bool horizontal)
        {
            var obstacleCell = cell.Plus(new Point(horizontal ? -direction.X : 0, horizontal ? 0 : -direction.Y));
            return _map[obstacleCell] && //obstacle position have to be covered by map due to geometry of rectangular grid 
                   _map.Covers(cell.Plus(direction.Perpendicular().Scale(horizontal ? 1 : -1))) &&
                   !_map[cell.Plus(direction.Perpendicular().Scale(horizontal ? 1 : -1))];
        }

	    static bool IsDiagonal(Point direction)
	    {
	        return (direction.X * direction.X + direction.Y * direction.Y) == 2;
	    }
	}
}