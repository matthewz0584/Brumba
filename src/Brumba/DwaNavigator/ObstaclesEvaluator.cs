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
        public ObstaclesEvaluator(IEnumerable<Vector2> obstacles, double robotRadius, double breakageDeceleration, double rangefinderMaxRange, double dt)
        {
            DC.Contract.Requires(robotRadius > 0);
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(obstacles.All(o => o.Length().BetweenR((float)robotRadius, (float)(rangefinderMaxRange + robotRadius))));
            DC.Contract.Requires(breakageDeceleration > 0);
            DC.Contract.Requires(dt > 0);

            Obstacles = obstacles;
            RobotRadius = robotRadius;
            BreakageDeceleration = breakageDeceleration;
            RangefinderMaxRange = rangefinderMaxRange;
            Dt = dt;
        }

        public IEnumerable<Vector2> Obstacles { get; private set; }
        public double RobotRadius { get; private set; }
        public double BreakageDeceleration { get; private set; }
        public double RangefinderMaxRange { get; private set; }
        public double Dt { get; private set; }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Assert(v.Linear >= 0);

            var dist = GetDistanceToClosestObstacle(v);
            if (double.IsPositiveInfinity(dist))
                return 1;
            if (!IsVelocityAdmissible(v, dist))
                return double.NegativeInfinity;
            return dist / ((RangefinderMaxRange + RobotRadius) / 2 * Constants.Pi - RobotRadius);
        }

        public double GetDistanceToClosestObstacle(Velocity v)
        {
            DC.Contract.Requires(v.Linear >= 0);
            DC.Contract.Ensures(double.IsPositiveInfinity(DC.Contract.Result<double>()) ||
                DC.Contract.Result<double>().BetweenRL(0, (RangefinderMaxRange + RobotRadius) / 2 * Constants.Pi - RobotRadius));

            //Obstacles are in robot coordinates system
            return (Math.Abs(v.Angular) <= 0.01 ?
                DistancesToObstaclesOnLine() :
                DistancesToObstaclesOnCircle(new CircleMotionModel(v))).
                Select(d => d < RobotRadius ? RobotRadius : d).
                DefaultIfEmpty(double.PositiveInfinity).Min() - RobotRadius;
        }

        public bool IsVelocityAdmissible(Velocity velocity, double distanceToObstacleOnLane)
        {
            DC.Contract.Requires(velocity.Linear >= 0);
            DC.Contract.Requires(distanceToObstacleOnLane >= 0);

            return velocity.Linear < -Dt * BreakageDeceleration +
                Math.Sqrt(Dt * Dt * BreakageDeceleration * BreakageDeceleration + 2 * distanceToObstacleOnLane * BreakageDeceleration);
        }

        public IEnumerable<double> DistancesToObstaclesOnLine()
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>().All(d => double.IsPositiveInfinity(d) || 
                d.BetweenRL(0, RobotRadius + RangefinderMaxRange)));

            return Obstacles.Where(o => o.Y <= RobotRadius && o.Y >= -RobotRadius && o.X >= 0).Select(o => (double)o.X);
        }

        public IEnumerable<double> DistancesToObstaclesOnCircle(CircleMotionModel cmm)
        {
            DC.Contract.Requires(cmm != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>().All(d => double.IsPositiveInfinity(d) ||
                d.BetweenRL(0, (RangefinderMaxRange + RobotRadius) / 2 * Constants.Pi)));

            return Obstacles.
                Where(o =>
                    PointIsInCircle(true, cmm.Center, o, Math.Abs(cmm.Radius) + RobotRadius) &&
                    ((Math.Abs(cmm.Radius) - RobotRadius) <= 0 || PointIsInCircle(false, cmm.Center, o, Math.Abs(cmm.Radius) - RobotRadius))).
                Select(o =>
                {
                    var angularDistanceAlongCircle = MathHelper2.AngleBetween(o - cmm.Center, -cmm.Center);
                    return Math.Abs(cmm.Radius) * (o.X < 0 ? Constants.Pi2 - angularDistanceAlongCircle : angularDistanceAlongCircle);
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