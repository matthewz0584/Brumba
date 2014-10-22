using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;
using NUnit.Framework;
using Matrix = Microsoft.Xna.Framework.Matrix;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class QqTests
    {
        [Test]
        public void DistanceToObstacleOnStraightTrajectory()
        {
            var dc = new DistanceCalculator(robotRadius: 1d);

            //No obstacles
            Assert.That(double.IsPositiveInfinity(dc.CalculateDistanceToObstacleOnCurve(linearVelocity: 1, angularVelocity: 0, obstacles: new DenseVector[0])));

            //Obstacles are not in the lane
            Assert.That(double.IsPositiveInfinity(dc.CalculateDistanceToObstacleOnCurve(1, 0, new[] { Vector(0, 10), Vector(0, -10) })));

            //Obstacle is on negative part of the lane
            Assert.That(double.IsPositiveInfinity(dc.CalculateDistanceToObstacleOnCurve(1, 0, new[] { Vector(-3, 0) })));

            //One obstacle staright ahead
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(1, 0, new[] { Vector(3, 0) }), Is.EqualTo(2));

            //One obstacle ahead and to the side
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(1, 0, new[] { Vector(3, 0.5) }), Is.EqualTo(2));

            //Two obstacles ahead, one is closer
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(1, 0, new[] { Vector(3, 0.5), Vector(4, -0.5) }), Is.EqualTo(2));

            //Two obstacles, one is not in the lane
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(1, 0, new[] { Vector(3, 0.5), Vector(2, -2) }), Is.EqualTo(2));

            //Obstacle is exactly on the border of the lane
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(1, 0, new[] { Vector(3, 1) }), Is.EqualTo(2));
        }

        [Test]
        public void DistanceToObstacleOnCurve()
        {
            var dc = new DistanceCalculator(1d);

            //Obstacle is not on the lane
            Assert.That(double.IsPositiveInfinity(dc.CalculateDistanceToObstacleOnCurve(50, 10, new[] {Vector(10, 10)})));

            //Obstacle is exactly on X axis
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(50, 10, new[] { Vector(0, 10) }), Is.EqualTo(Constants.Pi * (50 / 10) - 1));
            //Obstacle on positive part of the lane
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(50, 10, new[] { Vector(0.1, 10) }), Is.LessThan(Constants.Pi * (50 / 10) - 1));
            //Obstacle on negative part of the lane
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(50, 10, new[] { Vector(-0.1, 10) }), Is.GreaterThan(Constants.Pi * (50 / 10) - 1));
            //Obstacle is exactly on the border of lane
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(50, 10, new[] { Vector(0, 11) }), Is.EqualTo(Constants.Pi * (50 / 10) - 1));
            //Obstacle on positive part of the lane, but has negative X
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(50, 10, new[] { Vector(0.1, -0.5) }), Is.GreaterThan(0));
            //Curve down
            Assert.That(dc.CalculateDistanceToObstacleOnCurve(50, -10, new[] { Vector(0, -10) }), Is.EqualTo(Constants.Pi * (50 / 10) - 1));
        }

        public static DenseVector Vector(params double[] elements)
        {
            return new DenseVector(elements);
        }
    }

    public class DistanceCalculator
    {
        private readonly double _robotRadius;

        public DistanceCalculator(double robotRadius)
        {
            _robotRadius = robotRadius;
        }

        public double CalculateDistanceToObstacleOnCurve(double linearVelocity, double angularVelocity, IEnumerable<DenseVector> obstacles)
        {
            //contracts: linearVelocity > 0

            //obstacles are in robot coordinates
            var result = double.PositiveInfinity;
            if (angularVelocity == 0)
            {
                //-x<=R and x>=-R and y>=0 as a matrix "a" and vector "b"
                var a = new DenseMatrix(3, 2);
                a[0, 1] = -1;
                a[1, 1] = 1;
                a[2, 0] = -1;
                var b = QqTests.Vector(_robotRadius, _robotRadius, 0);
                foreach (var obstacle in obstacles.Where(o => (a*o - b).Maximum() <= 0))
                {
                    var nextDist = obstacle[0] - _robotRadius;
                    if (nextDist < result)
                        result = nextDist;
                }
            }
            else
            {
                var curveCenter = QqTests.Vector(0, linearVelocity / angularVelocity);
                var curveR = Math.Abs(curveCenter[1]);
                var curveRInner = curveR - _robotRadius;
                var curveROuter = curveR + _robotRadius;
                foreach (var obstacle in obstacles.Where(o =>
                    o[0] * o[0] + (o[1] - curveCenter[1]) * (o[1] - curveCenter[1]) >= curveRInner * curveRInner &&
                    o[0] * o[0] + (o[1] - curveCenter[1]) * (o[1] - curveCenter[1]) <= curveROuter * curveROuter))
                {
                    var fromCurveCenterToObstacle = obstacle - curveCenter;
                    var gamma = Math.Acos(fromCurveCenterToObstacle.Normalize(2) * (-curveCenter).Normalize(2));
                    gamma = fromCurveCenterToObstacle[0] < 0 ? Constants.Pi2 - gamma : gamma;
                    var nextDist = curveR * gamma - _robotRadius;
                    if (nextDist < result)
                        result = nextDist;
                }
            }
            return result;
        }
    }
}