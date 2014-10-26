using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class ObstaclesOnLaneDistanceCalculator
    {
        public ObstaclesOnLaneDistanceCalculator(double robotRadius)
        {
            DC.Contract.Requires(robotRadius > 0);

            RobotRadius = robotRadius;
        }

        public double RobotRadius { get; private set; }

        public double GetDistanceToClosestObstacle(Velocity v, IEnumerable<Vector2> obstacles)
        {
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(v.Linear > 0);
            DC.Contract.Requires(obstacles.All(o => o.Length() > RobotRadius));
            DC.Contract.Ensures(DC.Contract.Result<double>() > 0);

            //Obstacles are in robot coordinates system
            return (v.Angular == 0
                ? DistancesToObstaclesOnLine(obstacles)
                : DistancesToObstaclesOnCircle(new CircleMotionModel(v), obstacles)).
                DefaultIfEmpty(float.PositiveInfinity).Min() - RobotRadius;
        }

        public IEnumerable<double> DistancesToObstaclesOnLine(IEnumerable<Vector2> obstacles)
        {
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(obstacles.All(o => o.Length() > RobotRadius));
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>() != null);

            return obstacles.
                Where(o => o.Y <= RobotRadius && o.Y >= -RobotRadius && o.X >= 0).
                Select(o => (double)o.X);
        }

        public IEnumerable<double> DistancesToObstaclesOnCircle(CircleMotionModel cmm, IEnumerable<Vector2> obstacles)
        {
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(cmm != null);
            DC.Contract.Requires(obstacles.All(o => o.Length() > RobotRadius));
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<double>>() != null);

            return obstacles.
                Where(o =>
                    PointIsInCircle(false, cmm.Center, o, Math.Abs(cmm.Radius) - RobotRadius) &&
                    PointIsInCircle(true, cmm.Center, o, Math.Abs(cmm.Radius) + RobotRadius)).
                Select(o =>
                {
                    var angularDistanceAlongCircle = Math.Acos(Vector2.Dot(Vector2.Normalize(o - cmm.Center), Vector2.Normalize(-cmm.Center)));
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