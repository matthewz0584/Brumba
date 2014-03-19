using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Brumba.Utils;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class PoseHistogram
    {
        PoseBin[,,] _histogram;

        public PoseHistogram(OccupancyGrid map, double thetaBinSize)
        {
            Contract.Requires(map != null);
            Contract.Requires(map.SizeInCells.X > 0);
            Contract.Requires(map.SizeInCells.Y > 0);
            Contract.Requires(thetaBinSize > 0);
            Contract.Requires(thetaBinSize <= MathHelper.TwoPi);

            Map = map;
            ThetaBinSize = thetaBinSize;
        }

        public OccupancyGrid Map { get; private set; }

        public double ThetaBinSize { get; private set; }

        public IEnumerable<PoseBin> Bins { get { return _histogram.Cast<PoseBin>(); } }

        [Pure]
        public Vector3 Size
        {
            get { return new Vector3(Map.SizeInCells.X, Map.SizeInCells.Y, (int) Math.Ceiling(2 * Math.PI / ThetaBinSize)); }
        }

        public void Build(IEnumerable<Vector3> poseSamples)
        {
            Contract.Requires(poseSamples != null);
            Contract.Requires(poseSamples.Any());
            Contract.Ensures(_histogram.GetLength(0) == (int)Size.X);
            Contract.Ensures(_histogram.GetLength(1) == (int)Size.Y);
            Contract.Ensures(_histogram.GetLength(2) == (int)Size.Z);
            Contract.Ensures(Contract.ForAll(Bins, b => b != null));
            Contract.Ensures(Bins.Sum(pb => pb.Samples.Count()) <= poseSamples.Count());

            _histogram = new PoseBin[Map.SizeInCells.X, Map.SizeInCells.Y, (int)Size.Z];
            for (var x = 0; x < _histogram.GetLength(0); ++x)
                for (var y = 0; y < _histogram.GetLength(1); ++y)
                    for (var theta = 0; theta < _histogram.GetLength(2); ++theta)
                        _histogram[x, y, theta] = new PoseBin();
            foreach (var p in poseSamples.Where(ps => Map.Covers(ps.ExtractVector2())))
                this[p].AddSample(p);
        }

        public PoseBin this[Vector3 poseSample]
        {
            get
            {
                Contract.Requires(Map.Covers(poseSample.ExtractVector2()));
                Contract.Ensures(Contract.Result<PoseBin>() != null);

                var poseCell = Map.PosToCell(poseSample.ExtractVector2());
                return this[poseCell.X, poseCell.Y, (int)(poseSample.Z.ToPositiveAngle() / ThetaBinSize)];
            }
        }

        public PoseBin this[int xBin, int yBin, int thetaBin]
        {
            get
            {
                Contract.Requires(new Vector3(xBin, yBin, thetaBin).Between(new Vector3(), Size));
                Contract.Ensures(Contract.Result<PoseBin>() != null);

                return _histogram[xBin, yBin, thetaBin];
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var row = (int)Size.Y - 1; row >= 0; --row)
            {
                sb.AppendFormat("{0, -5}", row);
                for (var col = 0; col < (int)Size.X; ++col)
                {
                    var colRowBinMarginal = 0;
                    for (var thetaBin = 0; thetaBin < (int)Size.Z; ++thetaBin)
                        colRowBinMarginal += this[col, row, thetaBin].Samples.Count();
                    sb.AppendFormat("{0, 5}", colRowBinMarginal);
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendFormat("{0, -5}", "");
            Enumerable.Range(0, Map.SizeInCells.X).ToList().ForEach(colInd => sb.AppendFormat("{0, 5}", colInd));
            sb.AppendLine();
            return sb.ToString();
        }

        public class PoseBin
        {
            readonly List<Vector3> _samples = new List<Vector3>();

            public IEnumerable<Vector3> Samples
            {
                get { return _samples; }
            }

            public void AddSample(Vector3 poseSample)
            {
                Contract.Ensures(Samples.Count() == Contract.OldValue(Samples.Count()) + 1);

                _samples.Add(poseSample);
            }

            public Vector3 PoseMean()
            {
                Contract.Requires(Samples.Any());

                var position = _samples.Aggregate(new Vector2(), (sum, next) => next.ExtractVector2() + sum) / _samples.Count;
                var theta = MathHelper2.AngleMean(_samples.Select(s => s.Z));
                return new Vector3(position, theta);
            }
        }
    }
}