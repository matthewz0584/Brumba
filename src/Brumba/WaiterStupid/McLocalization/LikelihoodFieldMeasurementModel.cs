using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class LikelihoodFieldMeasurementModel : IMeasurementModel<Pose, IEnumerable<float>>
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
            Contract.Invariant(RangefinderProperties.AngularResolution > 0);
            Contract.Invariant(SigmaHit > 0);
            Contract.Invariant(WeightHit >= 0);
            Contract.Invariant(WeightRandom >= 0);
            Contract.Invariant((WeightHit + WeightRandom).AlmostEqualInDecimalPlaces(1, 5));            
        }

        public LikelihoodFieldMeasurementModel(OccupancyGrid map, RangefinderProperties rangefinderProperties, float sigmaHit, float weightHit, float weightRandom)
        {
            Contract.Requires(map != null);
            Contract.Requires(rangefinderProperties.MaxRange > 0);
            Contract.Requires(rangefinderProperties.AngularResolution > 0);
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

        public float ComputeMeasurementLikelihood(Pose robotPose, IEnumerable<float> scan)
        {
            Contract.Assume(scan != null);
            Contract.Assume(scan.Count() == RangefinderProperties.AngularRange / RangefinderProperties.AngularResolution + 1);

			var beamLikelihoods = scan.Select((zi, i) => new { zi, i }).Where(p => p.zi != RangefinderProperties.MaxRange).
				Select(p => BeamLikelihood(robotPose, p.zi, p.i)).ToList();
            var measurementLikelihood = beamLikelihoods.Aggregate(0.1f, (pi, p) => p + pi);
            
            //if (measurementLikelihood > 0.06)
            //{
            //    beamLikelihoods.ForEach(pi => Console.Write(" {0} ", pi));
            //    Console.WriteLine();
            //    Console.WriteLine("likelihood {0}", measurementLikelihood);
            //    Console.WriteLine("robotPose {0}", robotPose);
            //    Console.WriteLine("*****");
            //}

            //return scan.Select((zi, i) => new {zi, i}).Where(p => p.zi != RangefinderProperties.MaxRange).
            //    Select(p => BeamLikelihood(robotPose, p.zi, p.i)).Aggregate(1f, (pi, p) => p * pi);
	        return measurementLikelihood;
        }

        public float BeamLikelihood(Pose robotPose, float zi, int i)
        {
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= RangefinderProperties.MaxRange);
            Contract.Requires(i >= 0);
            Contract.Ensures(Contract.Result<float>() >= 0);

            var beamEndPointPosition = BeamEndPointPosition(robotPose, zi, i);
	        if (!Map.Covers(beamEndPointPosition))
		        return 0;

            return Vector2.Dot(
                new Vector2((float)DensityHit(DistanceToNearestObstacle(beamEndPointPosition)), (float)DensityRandom()),
                new Vector2(WeightHit, WeightRandom));
        }

        double DensityHit(float distanceToObstacle)
        {
            Contract.Requires(distanceToObstacle >= 0);
            Contract.Ensures(Contract.Result<double>() >= 0);

            return new Normal(0, SigmaHit).Density(distanceToObstacle);
        }

        double DensityRandom()
        {
            Contract.Requires(RangefinderProperties.MaxRange > 0);
            Contract.Ensures(Contract.Result<double>() >= 0);

            return 1d / RangefinderProperties.MaxRange;
        }

        public Vector2 BeamEndPointPosition(Pose robotPose, float zi, int i)
        {
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= RangefinderProperties.MaxRange);
            Contract.Requires(i >= 0);
            Contract.Requires(i < RangefinderProperties.AngularRange / RangefinderProperties.AngularResolution + 1);

            return RobotToMapTransformation(RangefinderProperties.BeamToVectorInRobotTransformation(zi, i), robotPose);
        }

        public static Vector2 RobotToMapTransformation(Vector2 beam, Pose robotPose)
        {
            return Vector2.Transform(beam, Matrix.CreateRotationZ((float)robotPose.Bearing) * Matrix.CreateTranslation(new Vector3(robotPose.Position, 0)));
        }

        public float DistanceToNearestObstacle(Vector2 position)
        {
            Contract.Requires(Map.Covers(position));
            Contract.Ensures(Contract.Result<float>() >= 0);
            Contract.Ensures(float.IsPositiveInfinity(Contract.Result<float>()) || Contract.Result<float>() <= Math.Sqrt(Map.SizeInCells.X * Map.SizeInCells.X + Map.SizeInCells.Y * Map.SizeInCells.Y) * Map.CellSize);

            if (Map[position])
                return 0;
            var nearestOccupiedCell = FindNearestOccupiedCell(position);
            if (nearestOccupiedCell == new Point(-1, -1))
                return float.PositiveInfinity;
            var distToCellCenter = (Map.CellToPos(nearestOccupiedCell) - position).Length();
            //Approximation of occupied cell by occupied circle with radius equal to average between inscribed and circumscribed about the cell circles: a*(1/2 + 1/sq(2))/2=0.6a
            return (distToCellCenter - 0.6f * Map.CellSize) > 0 ? distToCellCenter - 0.6f * Map.CellSize : 0;
        }

        Point FindNearestOccupiedCell(Vector2 position)
        {
            Contract.Requires(Map.Covers(position));
            Contract.Ensures(Contract.Result<Point>() == new Point(-1, -1) || Map.Covers(Contract.Result<Point>()));
            Contract.Ensures(Contract.Result<Point>() == new Point(-1, -1) || Map[Contract.Result<Point>()]);

            var circleFringe = new GridCircleFringeGenerator(Map.SizeInCells);
            var radius = 0;
            IEnumerable<Point> fringe;
            do
            {
                fringe = circleFringe.Generate(Map.PosToCell(position), radius++).ToList();
                if (fringe.Any(p => Map[p]))
                    return fringe.Where(p => Map[p]).OrderBy(p => (Map.CellToPos(p) - position).Length()).First();
            } while (fringe.Any());

            return new Point(-1, -1);
        }
    }
}