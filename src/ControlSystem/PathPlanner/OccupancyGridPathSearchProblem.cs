using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Brumba.Utils;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
    public class OccupancyGridPathSearchProblem : ISearchProblem<Point>
    {
        private Point _goalState;

        public OccupancyGridPathSearchProblem(OccupancyGrid map, ICellExpander cellExpander)
        {
            DC.Contract.Requires(map != null);
            DC.Contract.Requires(cellExpander != null);

            Map = map;
            CellExpander = cellExpander;
        }

        public ICellExpander CellExpander { get; private set; }
        
        public Point InitialState { get; set; }

        public Point GoalState
        {
            get { return _goalState; }
            set
            {
                DC.Contract.Assert(CellExpander != null);

                _goalState = value;
                CellExpander.Goal = value;
            }
        }

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