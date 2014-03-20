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

		public IEnumerable<PoseBin> Bins { get { return _histogram == null ? null : _histogram.Cast<PoseBin>(); } }

        [Pure]
        public Vector3 Size
        {
            get { return new Vector3(Map.SizeInCells.X, Map.SizeInCells.Y, (int) Math.Ceiling(2 * Math.PI / ThetaBinSize)); }
        }

        public void Build(IEnumerable<Pose> poseSamples)
        {
            Contract.Requires(poseSamples != null);
            Contract.Requires(poseSamples.Any());
			Contract.Ensures(_histogram.Rank == 3);
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
            foreach (var p in poseSamples.Where(ps => Map.Covers(ps.Position)))
                this[p].AddSample(p);
        }

        public PoseBin this[Pose poseSample]
        {
            get
            {
				Contract.Requires(Bins != null);
                Contract.Requires(Map.Covers(poseSample.Position));
                Contract.Ensures(Contract.Result<PoseBin>() != null);

                var poseCell = Map.PosToCell(poseSample.Position);
                return this[poseCell.X, poseCell.Y, (int)(poseSample.Bearing.ToPositiveAngle() / ThetaBinSize)];
            }
        }

        public PoseBin this[int xBin, int yBin, int thetaBin]
        {
            get
            {
				Contract.Requires(Bins != null);
                Contract.Requires(new Vector3(xBin, yBin, thetaBin).Between(new Vector3(), Size));
                Contract.Ensures(Contract.Result<PoseBin>() != null);

                return _histogram[xBin, yBin, thetaBin];
            }
        }

		public int[,] ToXyMarginal()
		{
			Contract.Requires(Bins != null);
			Contract.Ensures(Contract.Result<int[,]>().Rank == 2);
			Contract.Ensures(Contract.Result<int[,]>().GetLength(0) == (int)Size.X);
			Contract.Ensures(Contract.Result<int[,]>().GetLength(1) == (int)Size.Y);

			var xyM = new int[(int)Size.X, (int)Size.Y];
			for (var x = 0; x < (int)Size.X; ++x)
				for (var y = 0; y < (int)Size.Y; ++y)
					for (var thetaBin = 0; thetaBin < (int)Size.Z; ++thetaBin)
						xyM[x, y] += this[x, y, thetaBin].Samples.Count();
			return xyM;
		}

        public override string ToString()
        {
	        if (Bins == null)
		        return "None";

            var sb = new StringBuilder();
	        var xyM = ToXyMarginal();
            for (var row = (int)Size.Y - 1; row >= 0; --row)
            {
                sb.AppendFormat("{0, -5}", row);
                for (var col = 0; col < (int)Size.X; ++col)
                    sb.AppendFormat("{0, 5}", xyM[col, row]);
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
            readonly List<Pose> _samples = new List<Pose>();

            public IEnumerable<Pose> Samples
            {
                get { return _samples; }
            }

            public void AddSample(Pose poseSample)
            {
                Contract.Ensures(Samples.Count() == Contract.OldValue(Samples.Count()) + 1);

                _samples.Add(poseSample);
            }

            public Pose CalculatePoseMean()
            {
                Contract.Requires(Samples.Any());

                var position = _samples.Aggregate(new Vector2(), (sum, next) => next.Position + sum) / _samples.Count;
                var bearing = MathHelper2.AngleMean(_samples.Select(s => s.Bearing));
                return new Pose(position, bearing);
            }
        }
    }
}