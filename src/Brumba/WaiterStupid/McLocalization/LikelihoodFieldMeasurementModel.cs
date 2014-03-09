using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Brumba.Utils;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class LikelihoodFieldMeasurementModel : IMeasurementModel<Vector3, IEnumerable<float>>
    {
        public OccupancyGrid Map { get; private set; }
        public RangefinderProperties RangefinderProperties { get; private set; }
        public float ZeroBeamAngle { get; private set; }
        public float SigmaHit { get; private set; }
        public float WeightHit { get; private set; }
        public float WeightRandom { get; private set; }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Map != null);
            Contract.Invariant(RangefinderProperties != null);
            Contract.Invariant(RangefinderProperties.MaxRange > 0);
            Contract.Invariant(ZeroBeamAngle >= 0);
            Contract.Invariant(ZeroBeamAngle < MathHelper.TwoPi);
            Contract.Invariant(SigmaHit > 0);
            Contract.Invariant(WeightHit >= 0);
            Contract.Invariant(WeightRandom >= 0);
            Contract.Invariant((WeightHit + WeightRandom).AlmostEqualInDecimalPlaces(1, 5));            
        }

        public LikelihoodFieldMeasurementModel(OccupancyGrid map, RangefinderProperties rangefinderProperties, float zeroBeamAngle, float sigmaHit, float weightHit, float weightRandom)
        {
            Contract.Requires(map != null);
            Contract.Requires(rangefinderProperties != null);
            Contract.Requires(rangefinderProperties.MaxRange > 0);
            Contract.Requires(zeroBeamAngle >= 0);
            Contract.Requires(zeroBeamAngle < MathHelper.TwoPi);
            Contract.Requires(sigmaHit > 0);
            Contract.Requires(weightHit >= 0);
            Contract.Requires(weightRandom >= 0);
            Contract.Requires((weightHit + weightRandom).AlmostEqualInDecimalPlaces(1, 5));

            Map = map;
            RangefinderProperties = rangefinderProperties;
            ZeroBeamAngle = zeroBeamAngle;
            SigmaHit = sigmaHit;
            WeightHit = weightHit;
            WeightRandom = weightRandom;
        }

        public float ComputeMeasurementLikelihood(Vector3 robotPose, IEnumerable<float> scan)
        {
            Contract.Assume(scan != null);
            Contract.Assume(new Vector2(robotPose.X, robotPose.Y).Between(new Vector2(), Map.SizeInMeters));

            return scan.Select((zi, i) => new {zi, i}).Where(p => p.zi != RangefinderProperties.MaxRange).
                    Select(p => BeamLikelihood(robotPose, p.zi, p.i)).Aggregate(1f, (pi, p) => p * pi);
        }

        public float BeamLikelihood(Vector3 robotPose, float zi, int i)
        {
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= RangefinderProperties.MaxRange);
            Contract.Requires(i >= 0);
            Contract.Requires(new Vector2(robotPose.X, robotPose.Y).Between(new Vector2(), Map.SizeInMeters));
            Contract.Ensures(Contract.Result<float>() >= 0);

            var beamEndPointPosition = BeamEndPointPosition(zi, i, robotPose);
            if (!beamEndPointPosition.Between(new Vector2(), Map.SizeInMeters))
                return 1;

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
            Contract.Requires(new Vector2(robotPose.X, robotPose.Y).Between(new Vector2(), Map.SizeInMeters));

            return RobotToMapTransformation(RangefinderProperties.BeamToVectorInRobotTransformation(zi, i, ZeroBeamAngle), robotPose);
        }

        public static Vector2 RobotToMapTransformation(Vector2 beam, Vector3 robotPose)
        {
            return Vector2.Transform(beam, Matrix.CreateRotationZ(robotPose.Z) * Matrix.CreateTranslation(new Vector3(robotPose.X, robotPose.Y, 0)));
        }

        public float DistanceToNearestObstacle(Vector2 position)
        {
            Contract.Requires(position.Between(new Vector2(), Map.SizeInMeters));
            Contract.Ensures(Contract.Result<float>() >= 0);
            Contract.Ensures(Contract.Result<float>() <= Math.Sqrt(Map.SizeInCells.X * Map.SizeInCells.X + Map.SizeInCells.Y * Map.SizeInCells.Y) * Map.CellSize);

            return (Map.CellToPos(FindNearestOccupiedCell(Map.PosToCell(position))) - position).Length();
        }

        Point FindNearestOccupiedCell(Point cell)
        {
            Contract.Requires(cell.Between(new Point(), Map.SizeInCells));
            Contract.Ensures(Contract.Result<Point>().Between(new Point(), Map.SizeInCells));

            return new GridSquareFringeGenerator(Map.SizeInCells).Generate(cell).First(p => Map[p]);
        }
    }
}