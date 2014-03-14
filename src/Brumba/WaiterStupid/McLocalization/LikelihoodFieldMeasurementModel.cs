using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class LikelihoodFieldMeasurementModel : IMeasurementModel<Vector3, IEnumerable<float>>
    {
        public OccupancyGrid Map { get; private set; }
        public RangefinderProperties RangefinderProperties { get; private set; }
        public float SigmaHit { get; private set; }
        public float WeightHit { get; private set; }
        public float WeightRandom { get; private set; }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Map != null);
            Contract.Invariant(RangefinderProperties.MaxRange > 0);
            Contract.Invariant(SigmaHit > 0);
            Contract.Invariant(WeightHit >= 0);
            Contract.Invariant(WeightRandom >= 0);
            Contract.Invariant((WeightHit + WeightRandom).AlmostEqualInDecimalPlaces(1, 5));            
        }

        public LikelihoodFieldMeasurementModel(OccupancyGrid map, RangefinderProperties rangefinderProperties, float sigmaHit, float weightHit, float weightRandom)
        {
            Contract.Requires(map != null);
            Contract.Requires(rangefinderProperties.MaxRange > 0);
            Contract.Requires(sigmaHit > 0);
            Contract.Requires(weightHit >= 0);
            Contract.Requires(weightRandom >= 0);
            Contract.Requires((weightHit + weightRandom).AlmostEqualInDecimalPlaces(1, 5));

            Map = map;
            RangefinderProperties = rangefinderProperties;
            SigmaHit = sigmaHit;
            WeightHit = weightHit;
            WeightRandom = weightRandom;
        }

        public float ComputeMeasurementLikelihood(Vector3 robotPose, IEnumerable<float> scan)
        {
            Contract.Assume(scan != null);

			var lis = scan.Select((zi, i) => new { zi, i }).Where(p => p.zi != RangefinderProperties.MaxRange).
				Select(p => BeamLikelihood(robotPose, p.zi, p.i)).ToList();
	        if (lis.Aggregate(1f, (pi, p) => p*pi) > 0.05)
	        {
		        lis.ForEach(pi => Console.Write(" {0} ", pi));
		        Console.WriteLine("likelihood {0}", lis.Aggregate(1f, (pi, p) => p*pi));
				Console.WriteLine("robotPose {0}", robotPose);
				Console.WriteLine("*****");
	        }
	        //Console.WriteLine("<1 {0} ** > 1 {1} ** = 1 {2}", qq.Count(l => l < 1), qq.Count(l => l > 1), qq.Count(l => l == 1));

            //return scan.Select((zi, i) => new {zi, i}).Where(p => p.zi != RangefinderProperties.MaxRange).
//				Select(p => BeamLikelihood(robotPose, p.zi, p.i)).Aggregate(1f, (pi, p) => p * pi);
	        return lis.Aggregate(1f, (pi, p) => p*pi);
        }

        public float BeamLikelihood(Vector3 robotPose, float zi, int i)
        {
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= RangefinderProperties.MaxRange);
            Contract.Requires(i >= 0);
            Contract.Ensures(Contract.Result<float>() >= 0);

            var beamEndPointPosition = BeamEndPointPosition(zi, i, robotPose);
	        if (!Map.Covers(beamEndPointPosition))
		        return (float)new Normal(0, SigmaHit).Density(0) / 2;

            return Vector2.Dot(
                new Vector2(DensityHit(DistanceToNearestObstacle(beamEndPointPosition)), DensityRandom()),
                new Vector2(WeightHit, WeightRandom));
        }

        float DensityHit(float distanceToObstacle)
        {
            Contract.Requires(distanceToObstacle >= 0);
            Contract.Ensures(Contract.Result<float>() >= 0);

            return (float)new Normal(0, SigmaHit).Density(distanceToObstacle);
        }

        float DensityRandom()
        {
            Contract.Requires(RangefinderProperties.MaxRange > 0);
            Contract.Ensures(Contract.Result<float>() >= 0);

            return 1f / RangefinderProperties.MaxRange;
        }

        public Vector2 BeamEndPointPosition(float zi, int i, Vector3 robotPose)
        {
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= RangefinderProperties.MaxRange);
            Contract.Requires(i >= 0);

            return RobotToMapTransformation(RangefinderProperties.BeamToVectorInRobotTransformation(zi, i), robotPose);
        }

        public static Vector2 RobotToMapTransformation(Vector2 beam, Vector3 robotPose)
        {
            return Vector2.Transform(beam, Matrix.CreateRotationZ(robotPose.Z) * Matrix.CreateTranslation(new Vector3(robotPose.X, robotPose.Y, 0)));
        }

        public float DistanceToNearestObstacle(Vector2 position)
        {
            Contract.Requires(Map.Covers(position));
            Contract.Ensures(Contract.Result<float>() >= 0);
            Contract.Ensures(Contract.Result<float>() <= Math.Sqrt(Map.SizeInCells.X * Map.SizeInCells.X + Map.SizeInCells.Y * Map.SizeInCells.Y) * Map.CellSize);

            return (Map.CellToPos(FindNearestOccupiedCell(Map.PosToCell(position))) - position).Length();
        }

        Point FindNearestOccupiedCell(Point cell)
        {
            Contract.Requires(Map.Covers(cell));
            Contract.Ensures(Map.Covers(Contract.Result<Point>()));

            return new GridSquareFringeGenerator(Map.SizeInCells).Generate(cell).First(p => Map[p]);
        }
    }
}