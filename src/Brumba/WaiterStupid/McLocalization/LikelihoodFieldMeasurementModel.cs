using System.Collections.Generic;
using System.Linq;
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
            Map = map;
            RangefinderProperties = rangefinderProperties;
            ZeroBeamAngle = zeroBeamAngle;
            SigmaHit = sigmaHit;
            WeightHit = weightHit;
            WeightRandom = weightRandom;
        }

        public float ScanProbability(IEnumerable<float> scan, Vector3 robotPose)
        {
            return scan.Select((zi, i) => BeamProbability(zi, i, robotPose)).Aggregate(1f, (pi, p) => p*pi);
        }

        public float BeamProbability(float zi, int i, Vector3 robotPose)
        {
            if (zi == RangefinderProperties.MaxRange)
                return 1;
            return Vector2.Dot(
                new Vector2(DensityHit(zi, i, robotPose), DensityRandom()),
                new Vector2(WeightHit, WeightRandom));
        }

        float DensityHit(float zi, int i, Vector3 robotPose)
        {
            return (float)new Normal(0, SigmaHit).Density(DistanceToNearestObstacle(BeamEndPointPosition(zi, i, robotPose)));
        }

        float DensityRandom()
        {
            return 1f / RangefinderProperties.MaxRange;
        }

        public Vector2 BeamEndPointPosition(float zi, int i, Vector3 robotPose)
        {
            return RobotToMapTransformation(RangefinderProperties.BeamToVectorInRobotTransformation(zi, i, ZeroBeamAngle), robotPose);
        }

        public static Vector2 RobotToMapTransformation(Vector2 beam, Vector3 robotPose)
        {
            return Vector2.Transform(beam, Matrix.CreateRotationZ(robotPose.Z) * Matrix.CreateTranslation(new Vector3(robotPose.X, robotPose.Y, 0)));
        }

        public float DistanceToNearestObstacle(Vector2 position)
        {
            return (Map.CellToPos(FindNearestOccupiedCell(Map.PosToCell(position))) - position).Length();
        }

        Point FindNearestOccupiedCell(Point point)
        {
            return new GridSquareFringeGenerator(Map.Size).Generate(point).First(p => Map[p]);
        }
    }
}