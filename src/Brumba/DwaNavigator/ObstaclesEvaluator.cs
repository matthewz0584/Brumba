using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class ObstaclesEvaluator : IVelocityEvaluator
    {
        public ObstaclesEvaluator(IEnumerable<Vector2> obstacles, double robotRadius, double breakageDeceleration, double laneWidthCoef, double dt)
        {
            DC.Contract.Requires(robotRadius > 0);
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(breakageDeceleration > 0);
            DC.Contract.Requires(laneWidthCoef >= 1);
            DC.Contract.Requires(dt > 0);

            Obstacles = obstacles;
            RobotRadius = robotRadius;
            BreakageDeceleration = breakageDeceleration;
            LaneWidthCoef = laneWidthCoef;
            Dt = dt;
        }

        public IEnumerable<Vector2> Obstacles { get; private set; }
        public double RobotRadius { get; private set; }
        public double BreakageDeceleration { get; private set; }
        public double LaneWidthCoef { get; private set; }
        public double Dt { get; private set; }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Assert(v.Linear >= 0);

            var dist = GetDistanceToClosestObstacle(v);
            if (double.IsPositiveInfinity(dist))
                return 1;
            if (!IsVelocityAdmissible(v, dist))
                return double.NegativeInfinity;
            return 1 / (-dist / (4*RobotRadius) - 1) + 1;
        }

        public double GetDistanceToClosestObstacle(Velocity v)
        {
            DC.Contract.Requires(v.Linear >= 0);
            DC.Contract.Ensures(DC.Contract.Result<double>() >= 0);

            //Obstacles are in robot coordinates system
            return (v.IsRectilinear ? DistancesToObstaclesOnLine() : DistancesToObstaclesOnCircle(new CirclularMotionModel(v))).
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
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>().All(d => d > 0));

            return Obstacles.Where(o => o.Y <= LaneWidthCoef * RobotRadius && o.Y >= -LaneWidthCoef * RobotRadius && o.X >= 0).Select(o => (double)o.X);
        }

        public IEnumerable<double> DistancesToObstaclesOnCircle(CirclularMotionModel cmm)
        {
            DC.Contract.Requires(cmm != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>().All(d => d >= 0));

            return Obstacles.
                Where(o =>
                    PointIsInCircle(true, cmm.Center, o, Math.Abs(cmm.Radius) + LaneWidthCoef * RobotRadius) &&
                    ((Math.Abs(cmm.Radius) - LaneWidthCoef * RobotRadius) <= 0 || PointIsInCircle(false, cmm.Center, o, Math.Abs(cmm.Radius) - LaneWidthCoef * RobotRadius))).
                Select(o =>
                {
                    var angularDistanceAlongCircle = MathHelper2.AngleBetween(o - cmm.Center, -cmm.Center);
                    return Math.Abs(cmm.Radius) * (o.X < 0 ? Constants.Pi2 - angularDistanceAlongCircle : angularDistanceAlongCircle);
                });
        }

        public bool IsStraightPathClear(Vector2 target)
        {
            var normal = Vector2.Normalize(Vector2.Transform(target, Matrix.CreateRotationZ(MathHelper.PiOver2)));

            return !Obstacles.Any(ob =>
                Vector2.Dot(target, ob) >= 0 &&
                Vector2.Dot(target, ob - target) <= 0 &&
                Vector2.Dot(normal, ob - normal * (float)RobotRadius) <= 0 &&
                Vector2.Dot(normal, ob + normal * (float)RobotRadius) >= 0);
        }

        static bool PointIsInCircle(bool inside, Vector2 curveCenter, Vector2 point, double radius)
        {
            DC.Contract.Requires(radius > 0);

            Func<double, double, bool> InsideOrOutside = (a, b) => inside ? a <= b : a >= b;
            return InsideOrOutside((point.X - curveCenter.X) * (point.X - curveCenter.X) + (point.Y - curveCenter.Y) * (point.Y - curveCenter.Y), radius * radius);
        }
    }
}