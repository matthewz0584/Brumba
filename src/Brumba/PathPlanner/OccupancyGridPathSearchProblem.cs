using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Brumba.Utils;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
    public interface IOccupancyGridPathSearchProblem : ISearchProblem<Point>
    {
        OccupancyGrid Map { get; }
    }

    public class OccupancyGridPathSearchProblem : IOccupancyGridPathSearchProblem
    {
        public OccupancyGridPathSearchProblem(OccupancyGrid map, Point start, Point goal)
        {
            DC.Contract.Requires(map != null);

            Map = map;
            InitialState = start;
            GoalState = goal;
        }

        public IStateExpander CellExpander { get; set; }
        
        public Point InitialState { get; set; }
        public Point GoalState { get; set; }
        
        public OccupancyGrid Map { get; private set; }

        public IEnumerable<Tuple<Point, int>> Expand(Point state)
        {
            return CellExpander.Expand(state).Select(c => Tuple.Create(c, Distance(c, state)));
        }

        public int GetHeuristic(Point state)
        {
            return Distance(GoalState, state);
        }

        static int Distance(Point lhs, Point rhs)
        {
            DC.Contract.Ensures(DC.Contract.Result<int>() >= 0);

            return rhs.Minus(lhs).LengthSq();
        }
    }
}