using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using Microsoft.Xna.Framework;

namespace Brumba.DwaNavigator
{
    public class ObstaclesOnLaneDistanceCalculator
    {
        private readonly double _robotRadius;

        public ObstaclesOnLaneDistanceCalculator(double robotRadius)
        {
            _robotRadius = robotRadius;
        }

        public double GetDistanceToClosestObstacle(double linearVelocity, double angularVelocity, IEnumerable<Vector2> obstacles)
        {
            return (angularVelocity == 0
                ? DistancesToObstaclesOnLine(obstacles)
                : DistancesToObstaclesOnCircle(linearVelocity / angularVelocity, obstacles)).
                DefaultIfEmpty(float.PositiveInfinity).Min() - _robotRadius;
        }

        public IEnumerable<double> DistancesToObstaclesOnLine(IEnumerable<Vector2> obstacles)
        {
            //contracts: linearVelocity > 0
            //ensures > 0

            //obstacles are in robot coordinates
            return obstacles.
                Where(o => o.Y <= _robotRadius && o.Y >= -_robotRadius && o.X >= 0).
                Select(o => (double)o.X);
        }

        public IEnumerable<double> DistancesToObstaclesOnCircle(double circleRadius, IEnumerable<Vector2> obstacles)
        {
            var circleCenter = new Vector2(0, (float)circleRadius);

            return obstacles.
                Where(o => 
                    PointIsInCircle(false, circleCenter, o, Math.Abs(circleRadius) - _robotRadius) &&
                    PointIsInCircle(true, circleCenter, o, Math.Abs(circleRadius) + _robotRadius)).
                Select(o =>
                {
                    var angularDistanceAlongCircle = Math.Acos(Vector2.Dot(Vector2.Normalize(o - circleCenter), Vector2.Normalize(-circleCenter)));
                    return Math.Abs(circleRadius) * (o.X < 0 ? Constants.Pi2 - angularDistanceAlongCircle : angularDistanceAlongCircle);
                });
        }

        static bool PointIsInCircle(bool inside, Vector2 curveCenter, Vector2 point, double radius)
        {
            Func<double, double, bool> InsideOrOutside = (a, b) => inside ? a <= b : a >= b;
            return InsideOrOutside((point.X - curveCenter.X) * (point.X - curveCenter.X) + (point.Y - curveCenter.Y) * (point.Y - curveCenter.Y), radius * radius);
        }
    }
}