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
        public OccupancyGridPathSearchProblem(OccupancyGrid map)
        {
            DC.Contract.Requires(map != null);

            Map = map;
        }

        public IStateExpander CellExpander { get; set; }
        
        public Point InitialState { get; set; }
        public Point GoalState { get; set; }
        
        public OccupancyGrid Map { get; private set; }

        public IEnumerable<Tuple<Point, double>> Expand(Point state)
        {
            DC.Contract.Assert(CellExpander != null);

            return CellExpander.Expand(state).Select(c => Tuple.Create(c, Distance(c, state)));
        }

        public double GetHeuristic(Point state)
        {
            return Distance(GoalState, state);
        }

        static double Distance(Point lhs, Point rhs)
        {
            DC.Contract.Ensures(DC.Contract.Result<double>() >= 0);

            return rhs.Minus(lhs).Length();
        }
    }
}