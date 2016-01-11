using System.Linq;
using Brumba.Common;
using Brumba.DiffDriveOdometry;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class ObstaclesEvaluatorTests
    {
        [Test]
        public void DistancesToObstaclesOnLine()
        {
            //No obstacles
            Assert.That(new ObstaclesEvaluator(new Vector2[0], 1, 1, 1, 0.25).
                DistancesToObstaclesOnLine(), Is.Empty);

            //Obstacles are not in the lane
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, 10), new Vector2(0, -10), new Vector2(-3, 0) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnLine(), Is.Empty);

            //One obstacle staright ahead
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(3, 0) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnLine().Single(), Is.EqualTo(3));

            //One obstacle ahead and to the side
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(3, 0.5f) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnLine().Single(), Is.EqualTo(3));

            //Obstacle is exactly on the border of the lane
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(3, 1) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnLine().Single(), Is.EqualTo(3));
            
            //Two obstacles ahead, one is closer
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(3, 0.5f), new Vector2(4, -0.5f) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnLine(), Is.EquivalentTo(new[] { 3d, 4 }));

            //Obstacles are in the wider lane
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(5, 10), new Vector2(5, -10) }, 1, 1, 10, 0.25).
                DistancesToObstaclesOnLine().Count(), Is.EqualTo(2));
        }

        [Test]
        public void DistancesToObstaclesOnCircle()
        {
            var cmm = new CirclularMotionModel(new Velocity(5, 1));

            //No obstacles
            Assert.That(new ObstaclesEvaluator(new Vector2[0], 1, 1, 1, 0.25).DistancesToObstaclesOnCircle(cmm), Is.Empty);

            //Obstacle is not on the lane
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(10, 10) }, 1, 1, 1, 0.25).DistancesToObstaclesOnCircle(cmm), Is.Empty);

            //Obstacle is exactly on X axis
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, 10) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.EqualTo(Constants.Pi * 5));
            //Obstacle on positive part of the lane
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0.1f, 10) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.LessThan(Constants.Pi * 5));
            //Obstacle on negative part of the lane
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(-0.1f, 10) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.GreaterThan(Constants.Pi * 5));
            //Obstacle is exactly on the border of lane
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, 11) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.EqualTo(Constants.Pi * 5));
            //Obstacle on positive part of the lane, but has negative Y
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(1.1f, -0.05f) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.GreaterThan(0));
            //Curve down
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, -10) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnCircle(new CirclularMotionModel(new Velocity(5, -1))).Single(), Is.EqualTo(Constants.Pi * 5));
            //Two obstacles are exactly on X axis
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, 9), new Vector2(0, 11) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnCircle(cmm), Is.EquivalentTo(new[] {Constants.Pi * 5, Constants.Pi * 5}));
            //Turning almost on place, there is no circle with (R - Rrobot) radius
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, 1.5f) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnCircle(new CirclularMotionModel(new Velocity(0.5, 1))).Single(), Is.EqualTo(Constants.Pi * 0.5));
            //Turning on place
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, 1.5f) }, 1, 1, 1, 0.25).
                DistancesToObstaclesOnCircle(new CirclularMotionModel(new Velocity(0, 1))), Is.Empty);
            //Obstacle on the wider lane
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0.1f, 14) }, 1, 1, 5, 0.25).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.LessThan(Constants.Pi * 5));
        }

        [Test]
        public void GetDistanceToClosestObstacle()
        {
            //No obstacles ahead, moving on line
            Assert.That(double.IsPositiveInfinity(new ObstaclesEvaluator(obstacles: new Vector2[0], robotRadius: 1d, breakageDeceleration: 1d, laneWidthCoef: 1d, dt: 0.25).
                GetDistanceToClosestObstacle(new Velocity(50, 0))));

            //One obstacle staright ahead, moving on line
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(3, 0) }, 1, 1, 1, 0.25).
                GetDistanceToClosestObstacle(new Velocity(50, 0)), Is.EqualTo(2));

            //Obstacle is exactly on X axis, moving on circle
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, 10) }, 1, 1, 1, 0.25).
                GetDistanceToClosestObstacle(new Velocity(50, 10)), Is.EqualTo(Constants.Pi * 5 - 1));

            //Obstacle is exactly on X axis, moving on circle with negative angular velocity
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, -10) }, 1, 1, 1, 0.25).
                GetDistanceToClosestObstacle(new Velocity(50, -10)), Is.EqualTo(Constants.Pi * 5 - 1));

            //On very small curve radiuses approximating algorithm returns distances smaller than zero
            //although all distances to obstacles are greater than robot radius. It is fixed explicitly.
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0, 0.6f) }, 0.5, 1, 1, 0.25).
                GetDistanceToClosestObstacle(new Velocity(0.1, 1)), Is.EqualTo(0));

            //Obstacles near the borders of lane of straight pass could be closer than robot radius, fix for it
            Assert.That(new ObstaclesEvaluator(new[] { new Vector2(0.99f, 0.99f) }, 1, 1, 1, 0.25).
                GetDistanceToClosestObstacle(new Velocity(50, 0)), Is.EqualTo(0));
        }

        [Test]
        public void IsVelocityAdmissible()
        {
            var oe = new ObstaclesEvaluator(new Vector2[0], 1, 4, 1, 0.25);

            Assert.That(oe.IsVelocityAdmissible(new Velocity(1, 0), 1));
            Assert.That(oe.IsVelocityAdmissible(new Velocity(1, 1000), 1));

            Assert.That(oe.IsVelocityAdmissible(new Velocity(1, 0), 0.24), Is.False);
            Assert.That(oe.IsVelocityAdmissible(new Velocity(1, 0), 0.3), Is.False);
        }

        [Test]
        public void Evaluate()
        {
            var oe = new ObstaclesEvaluator(new[] { new Vector2(3, 0), new Vector2(3, 2) }, 1, 0.5, 1, 0.25);

            Assert.That(oe.Evaluate(new Velocity(0, 10)), Is.EqualTo(1));
            Assert.That(oe.Evaluate(new Velocity(1, 1)), Is.EqualTo(1));
            Assert.That(double.IsNegativeInfinity(oe.Evaluate(new Velocity(10, 0))));
            Assert.That(oe.Evaluate(new Velocity(1, 0)), Is.GreaterThan(0).And.LessThan(1));
            Assert.That(oe.Evaluate(new Velocity(1, 0.5)), Is.GreaterThan(oe.Evaluate(new Velocity(1, 0))).And.LessThan(1));
        }

        [Test]
        public void IsStraightPathClear()
        {
            var oe = new ObstaclesEvaluator(new[] { new Vector2(5, 2.1f), new Vector2(1.9f, 5)  }, 2, 0.5, 1, 0.25);

            Assert.That(oe.IsStraightPathClear(new Vector2(10, 0)));
            Assert.That(oe.IsStraightPathClear(new Vector2(10, 1)), Is.False);
            Assert.That(oe.IsStraightPathClear(new Vector2(0, 10)), Is.False);
            Assert.That(oe.IsStraightPathClear(new Vector2(-1, 10)));

            Assert.That(oe.IsStraightPathClear(new Vector2(0, -10)));
            Assert.That(oe.IsStraightPathClear(new Vector2(0, 3)));
        }
    }
}