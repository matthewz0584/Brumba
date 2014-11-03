using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class ObstaclesEvaluator : IVelocityEvaluator
    {
        public ObstaclesEvaluator(IEnumerable<Vector2> obstacles, double robotRadius, double linearDecelerationMax, double rangefinderMaxRange)
        {
            DC.Contract.Requires(robotRadius > 0);
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(obstacles.All(o => o.Length().BetweenR((float)robotRadius, (float)(rangefinderMaxRange + robotRadius))));
            DC.Contract.Requires(linearDecelerationMax > 0);

            Obstacles = obstacles;
            RobotRadius = robotRadius;
            LinearDecelerationMax = linearDecelerationMax;
            RangefinderMaxRange = rangefinderMaxRange;
        }

        public IEnumerable<Vector2> Obstacles { get; private set; }
        public double RobotRadius { get; private set; }
        public double LinearDecelerationMax { get; private set; }
        public double RangefinderMaxRange { get; private set; }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Assert(v.Linear >= 0);

            var dist = GetDistanceToClosestObstacle(v);
            if (double.IsPositiveInfinity(dist))
                return 1;
            if (!IsVelocityAdmissible(v, dist))
                return 0;
            return 0.5 * dist / ((RangefinderMaxRange + RobotRadius) * Constants.PiOver2 - RobotRadius);
        }

        public double GetDistanceToClosestObstacle(Velocity v)
        {
            DC.Contract.Requires(v.Linear >= 0);
            DC.Contract.Ensures(DC.Contract.Result<double>() > 0);

            //Obstacles are in robot coordinates system
            return (v.Angular == 0
                ? DistancesToObstaclesOnLine()
                : DistancesToObstaclesOnCircle(new CircleMotionModel(v))).
                DefaultIfEmpty(float.PositiveInfinity).Min() - RobotRadius;
        }

        public bool IsVelocityAdmissible(Velocity velocity, double distanceToObstacleOnLane)
        {
            DC.Contract.Requires(velocity.Linear >= 0);
            DC.Contract.Requires(distanceToObstacleOnLane > 0);

            return velocity.Linear <= Math.Sqrt(2 * distanceToObstacleOnLane * LinearDecelerationMax);
        }

        public IEnumerable<double> DistancesToObstaclesOnLine()
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>().All(d => d > RobotRadius));

            return Obstacles.
                Where(o => o.Y <= RobotRadius && o.Y >= -RobotRadius && o.X >= 0).
                Select(o => (double)o.X);
        }

        public IEnumerable<double> DistancesToObstaclesOnCircle(CircleMotionModel cmm)
        {
            DC.Contract.Requires(cmm != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>().All(d => d > RobotRadius));

            return Obstacles.
                Where(o =>
                    PointIsInCircle(true, cmm.Center, o, Math.Abs(cmm.Radius) + RobotRadius) &&
                    ((Math.Abs(cmm.Radius) - RobotRadius) <= 0 || PointIsInCircle(false, cmm.Center, o, Math.Abs(cmm.Radius) - RobotRadius))).
                Select(o =>
                {
                    var angularDistanceAlongCircle = Math.Acos(Vector2.Dot(Vector2.Normalize(o - cmm.Center), Vector2.Normalize(-cmm.Center)));
                    var distanceAlongCircle = Math.Abs(cmm.Radius) * (o.X < 0 ? Constants.Pi2 - angularDistanceAlongCircle : angularDistanceAlongCircle);
                    return distanceAlongCircle > RobotRadius ? distanceAlongCircle : RobotRadius * 1.00001;
                });
        }

        static bool PointIsInCircle(bool inside, Vector2 curveCenter, Vector2 point, double radius)
        {
            DC.Contract.Requires(radius > 0);

            Func<double, double, bool> InsideOrOutside = (a, b) => inside ? a <= b : a >= b;
            return InsideOrOutside((point.X - curveCenter.X) * (point.X - curveCenter.X) + (point.Y - curveCenter.Y) * (point.Y - curveCenter.Y), radius * radius);
        }
    }
}