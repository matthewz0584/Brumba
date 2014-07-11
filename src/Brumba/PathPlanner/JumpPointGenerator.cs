using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
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
			while (true)
			{
				var neighbour = StepToward(from, direction);
				if (!_map.Covers(neighbour))
					return null;
				if (_map[neighbour])
					return null;
				if (neighbour == _searchProblem.GoalState)
					return neighbour;
				if (HasForcedNeighbours(neighbour, from))
					return neighbour;
				from = neighbour;
			}
		}

		bool HasForcedNeighbours(Point nextRightNeighbour, Point from)
		{
			return	((_map.Covers(new Point(nextRightNeighbour.X, nextRightNeighbour.Y + 1)) && _map[new Point(nextRightNeighbour.X, nextRightNeighbour.Y + 1)]) ||
			      	 (_map.Covers(new Point(nextRightNeighbour.X, nextRightNeighbour.Y - 1)) && _map[new Point(nextRightNeighbour.X, nextRightNeighbour.Y - 1)])) &&
			      	_map.SizeInCells.X > nextRightNeighbour.X + 1 &&
			      	((_map.Covers(new Point(nextRightNeighbour.X + 1, nextRightNeighbour.Y - 1)) && !_map[new Point(nextRightNeighbour.X + 1, nextRightNeighbour.Y - 1)]) ||
			      	 (_map.Covers(new Point(nextRightNeighbour.X + 1, nextRightNeighbour.Y)) && !_map[new Point(nextRightNeighbour.X + 1, nextRightNeighbour.Y)]) ||
			      	 (_map.Covers(new Point(nextRightNeighbour.X + 1, nextRightNeighbour.Y + 1)) && !_map[new Point(nextRightNeighbour.X + 1, nextRightNeighbour.Y + 1)]));
		}

		static Point StepToward(Point from, Point direction)
		{
			return new Point(from.X + direction.X, from.Y + direction.Y);
		}
	}
}