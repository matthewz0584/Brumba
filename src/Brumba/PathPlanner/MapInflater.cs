using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Brumba.McLrfLocalizer;
using Brumba.Utils;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
    public class MapInflater
    {
        public OccupancyGrid Map { get; private set; }
        public double Delta { get; private set; }

        public MapInflater(OccupancyGrid map, double delta)
        {
            DC.Contract.Requires(map != null);
            DC.Contract.Requires(delta > 0);

            Map = map;
            Delta = delta;
        }

        public IEnumerable<Point> CellInflation()
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Point>>().Any());

            var cg = new GridCircleFringeGenerator();
            return Enumerable.Range(0, (int) Math.Ceiling(RadiusOfInflation / Map.CellSize))
                .SelectMany(radius => cg.Generate(radius).Where(ShouldBeInflated));
        }

        public OccupancyGrid Inflate()
        {
            DC.Contract.Ensures(DC.Contract.Result<OccupancyGrid>().SizeInCells == Map.SizeInCells);
            DC.Contract.Ensures(DC.Contract.Result<OccupancyGrid>().CellSize == Map.CellSize);

            return new OccupancyGrid(CellInflation().Select(ShiftMap).Aggregate(MergeOccupancies), Map.CellSize);
        }

        bool[,] ShiftMap(Point shift)
        {
            DC.Contract.Ensures(DC.Contract.Result<bool[,]>() != null);
            DC.Contract.Ensures(DC.Contract.Result<bool[,]>().GetLength(0) == Map.SizeInCells.Y &&
                                DC.Contract.Result<bool[,]>().GetLength(1) == Map.SizeInCells.X);

            var shiftedOccupancy = new bool[Map.SizeInCells.Y, Map.SizeInCells.X];
            foreach (var cellValue in Map)
            {
                var shiftedCell = cellValue.Item1.Plus(shift);
                if (Map.Covers(shiftedCell))
                    shiftedOccupancy[shiftedCell.Y, shiftedCell.X] = cellValue.Item2;
            }
            return shiftedOccupancy;
        }

        bool[,] MergeOccupancies(bool[,] to, bool[,] from)
        {
            DC.Contract.Requires(to != null);
            DC.Contract.Requires(from != null);
            DC.Contract.Ensures(to.GetLength(0) == from.GetLength(0) && to.GetLength(1) == from.GetLength(1));
            DC.Contract.Ensures(to.GetLength(0) == Map.SizeInCells.Y && to.GetLength(1) == Map.SizeInCells.X);
            DC.Contract.Ensures(DC.Contract.Result<bool[,]>() == to);

            foreach (var cellValue in Map)
                to[cellValue.Item1.Y, cellValue.Item1.X] |= from[cellValue.Item1.Y, cellValue.Item1.X];
            return to;
        }

        bool ShouldBeInflated(Point cell)
        {
            return (Map.CellToPos(cell) - Map.CellToPos(new Point())).Length() <= RadiusOfInflation;
        }

        double RadiusOfInflation
        {
            get
            {
                DC.Contract.Ensures(DC.Contract.Result<double>() > 0);

                return Map.CellSize * 0.6 + Delta;
            }
        }
    }
}