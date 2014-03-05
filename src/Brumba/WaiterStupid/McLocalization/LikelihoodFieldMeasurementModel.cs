using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class LikelihoodFieldMeasurementModel
    {
        public OccupancyGrid Map { get; private set; }
        public RangefinderProperties RangefinderProperties { get; private set; }
        public float ZeroBeamAngle { get; private set; }
        public float SigmaHit { get; private set; }
        public float WeightHit { get; private set; }
        public float WeightRandom { get; private set; }

        public LikelihoodFieldMeasurementModel(OccupancyGrid map, RangefinderProperties rangefinderProperties, float zeroBeamAngle, float sigmaHit, float weightHit, float weightRandom)
        {
            Contract.Requires(map != null);
            Contract.Requires(rangefinderProperties != null);
            Contract.Requires(rangefinderProperties.MaxRange > 0);
            Contract.Requires(map != null);
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

        public float ScanProbability(IEnumerable<float> scan, Vector3 robotPose)
        {
            Contract.Requires(scan != null);

            return scan.Select((zi, i) => zi == RangefinderProperties.MaxRange ? 1.0f : BeamProbability(zi, i, robotPose)).Aggregate(1f, (pi, p) => p * pi);
        }

        public float BeamProbability(float zi, int i, Vector3 robotPose)
        {
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= RangefinderProperties.MaxRange);
            Contract.Requires(i >= 0);

            return Vector2.Dot(
                new Vector2(DensityHit(zi, i, robotPose), DensityRandom()),
                new Vector2(WeightHit, WeightRandom));
        }

        float DensityHit(float zi, int i, Vector3 robotPose)
        {
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= RangefinderProperties.MaxRange);
            Contract.Requires(i >= 0);

            return (float)new Normal(0, SigmaHit).Density(DistanceToNearestObstacle(BeamEndPointPosition(zi, i, robotPose)));
        }

        float DensityRandom()
        {
            Contract.Requires(RangefinderProperties.MaxRange > 0);

            return 1f / RangefinderProperties.MaxRange;
        }

        public Vector2 BeamEndPointPosition(float zi, int i, Vector3 robotPose)
        {
            Contract.Requires(zi >= 0);
            Contract.Requires(zi <= RangefinderProperties.MaxRange);
            Contract.Requires(i >= 0);

            return RobotToMapTransformation(RangefinderProperties.BeamToVectorInRobotTransformation(zi, i, ZeroBeamAngle), robotPose);
        }

        public static Vector2 RobotToMapTransformation(Vector2 beam, Vector3 robotPose)
        {
            return Vector2.Transform(beam, Matrix.CreateRotationZ(robotPose.Z) * Matrix.CreateTranslation(new Vector3(robotPose.X, robotPose.Y, 0)));
        }

        public float DistanceToNearestObstacle(Vector2 position)
        {
            Contract.Requires(position.X >= 0);
            Contract.Requires(position.Y >= 0);
            Contract.Requires(position.X < Map.Size.X * Map.CellSize);
            Contract.Requires(position.Y < Map.Size.Y * Map.CellSize);
            Contract.Ensures(Contract.Result<float>() >= 0);
            Contract.Ensures(Contract.Result<float>() <= Math.Sqrt(Map.Size.X * Map.Size.X + Map.Size.Y * Map.Size.Y) * Map.CellSize);

            return (Map.CellToPos(FindNearestOccupiedCell(Map.PosToCell(position))) - position).Length();
        }

        Point FindNearestOccupiedCell(Point cell)
        {
            Contract.Requires(cell.X >= 0);
            Contract.Requires(cell.Y >= 0);
            Contract.Requires(cell.X < Map.Size.X);
            Contract.Requires(cell.Y < Map.Size.Y);
            Contract.Ensures(Contract.Result<Point>().X >= 0);
            Contract.Ensures(Contract.Result<Point>().X < Map.Size.X);
            Contract.Ensures(Contract.Result<Point>().Y >= 0);
            Contract.Ensures(Contract.Result<Point>().Y < Map.Size.Y);

            return new GridSquareFringeGenerator(Map.Size).Generate(cell).First(p => Map[p]);
        }
    }
}