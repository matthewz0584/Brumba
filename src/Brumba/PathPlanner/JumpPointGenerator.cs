using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Brumba.Utils;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
    [DC.ContractClassAttribute(typeof(IStateExpanderContract))]
    public interface IStateExpander
    {
        IEnumerable<Point> Expand(Point from);
    }

    [DC.ContractClassForAttribute(typeof(IStateExpander))]
    abstract class IStateExpanderContract : IStateExpander
    {
        public IEnumerable<Point> Expand(Point from)
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Point>>() != null);
            return default(IEnumerable<Point>);
        }
    }

    public class JumpPointGenerator : IStateExpander
    {
		static readonly List<Point> _directions = new List<Point>
		{
			new Point(1, 0), new Point(1, 1), new Point(0, 1), new Point(-1, 1),
			new Point(-1, 0), new Point(-1, -1), new Point(0, -1), new Point(1, -1)
		};

        public JumpPointGenerator(IOccupancyGridPathSearchProblem searchProblem)
		{
            DC.Contract.Requires(searchProblem != null);

			SearchProblem = searchProblem;
		}

        public IOccupancyGridPathSearchProblem SearchProblem { get; private set; }

		public IEnumerable<Point> Expand(Point from)
		{
            DC.Contract.Requires(SearchProblem.Map.Covers(from));
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Point>>() != null);

			return _directions.Select(dir => JumpToward(from, dir)).Where(jp => jp.HasValue).Select(jp => jp.Value);
		}

		Point? JumpToward(Point from, Point direction)
		{
            DC.Contract.Requires(Map.Covers(from));
            DC.Contract.Requires(direction.Between(new Point(-1, -1), new Point(2, 2)) && direction.LengthSq() != 0);
		    DC.Contract.Ensures(!DC.Contract.Result<Point?>().HasValue ||
		                        (Map.Covers(DC.Contract.Result<Point?>().Value) && !Map[DC.Contract.Result<Point?>().Value]));

			var nextCell = from.Plus(direction);
            if (!Map.Covers(nextCell) || Map[nextCell])
				return null;
			if ((nextCell == SearchProblem.GoalState) ||
			    HasForcedNeighbours(nextCell, direction) ||
			    (IsDiagonal(direction) &&
			        (JumpToward(nextCell, new Point(direction.X, 0)).HasValue ||
			        JumpToward(nextCell, new Point(0, direction.Y)).HasValue)))
			    return nextCell;
		    return JumpToward(nextCell, direction);
		}

		bool HasForcedNeighbours(Point cell, Point direction)
		{
            DC.Contract.Requires(Map.Covers(cell));
            DC.Contract.Requires(direction.Between(new Point(-1, -1), new Point(2, 2)) && direction.LengthSq() != 0);

		    return IsDiagonal(direction) ?
                HasForcedDiagonalNeighbour(cell, direction, horizontal: true) ||
                HasForcedDiagonalNeighbour(cell, direction, horizontal: false)
                :
                HasForcedStraightNeighbour(cell, direction, up: true) ||
                HasForcedStraightNeighbour(cell, direction, up: false);
		}

        bool HasForcedStraightNeighbour(Point cell, Point direction, bool up)
        {
            DC.Contract.Requires(Map.Covers(cell));
            DC.Contract.Requires(direction.Between(new Point(-1, -1), new Point(2, 2)) && direction.LengthSq() != 0);

            var obstacleCell = cell.Plus(direction.Perpendicular().Scale(up ? 1 : -1));
            return Map.Covers(obstacleCell) && Map[obstacleCell] &&
                   Map.Covers(obstacleCell.Plus(direction)) && !Map[obstacleCell.Plus(direction)];
        }

        bool HasForcedDiagonalNeighbour(Point cell, Point direction, bool horizontal)
        {
            DC.Contract.Requires(Map.Covers(cell));
            DC.Contract.Requires(direction.Between(new Point(-1, -1), new Point(2, 2)) && direction.LengthSq() != 0);

            var obstacleCell = cell.Plus(new Point(horizontal ? -direction.X : 0, horizontal ? 0 : -direction.Y));
            return Map[obstacleCell] && //obstacle position have to be covered by map due to geometry of rectangular grid 
                   Map.Covers(cell.Plus(direction.Perpendicular().Scale(horizontal ? 1 : -1))) &&
                   !Map[cell.Plus(direction.Perpendicular().Scale(horizontal ? 1 : -1))];
        }

	    static bool IsDiagonal(Point direction)
	    {
            DC.Contract.Requires(direction.Between(new Point(-1, -1), new Point(2, 2)) && direction.LengthSq() != 0);

	        return direction.LengthSq() == 2;
	    }

        OccupancyGrid Map { get { return SearchProblem.Map; } }
	}
}