using System.Linq;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class ObstaclesOnLaneDistanceCalculatorTests
    {
        [Test]
        public void DistancesToObstaclesOnLine()
        {
            //No obstacles
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new Vector2[0], 1, 1, 1).
                DistancesToObstaclesOnLine(), Is.Empty);

            //Obstacles are not in the lane
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(0, 10), new Vector2(0, -10), new Vector2(-3, 0) }, 1, 1, 1).
                DistancesToObstaclesOnLine(), Is.Empty);

            //One obstacle staright ahead
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(3, 0) }, 1, 1, 1).
                DistancesToObstaclesOnLine().Single(), Is.EqualTo(3));

            //One obstacle ahead and to the side
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(3, 0.5f) }, 1, 1, 1).
                DistancesToObstaclesOnLine().Single(), Is.EqualTo(3));

            //Obstacle is exactly on the border of the lane
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(3, 1) }, 1, 1, 1).
                DistancesToObstaclesOnLine().Single(), Is.EqualTo(3));
            
            //Two obstacles ahead, one is closer
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(3, 0.5f), new Vector2(4, -0.5f) }, 1, 1, 1).
                DistancesToObstaclesOnLine(), Is.EquivalentTo(new[] { 3d, 4 }));
        }

        [Test]
        public void DistancesToObstaclesOnCircle()
        {
            var cmm = new CircleMotionModel(new Velocity(5, 1));

            //No obstacles

            Assert.That(new ObstaclesOnLaneDistanceCalculator(new Vector2[0], 1, 1, 1).DistancesToObstaclesOnCircle(cmm), Is.Empty);

            //Obstacle is not on the lane
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(10, 10) }, 1, 1, 1).DistancesToObstaclesOnCircle(cmm), Is.Empty);

            //Obstacle is exactly on X axis
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(0, 10) }, 1, 1, 1).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.EqualTo(Constants.Pi * 5));
            //Obstacle on positive part of the lane
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(0.1f, 10) }, 1, 1, 1).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.LessThan(Constants.Pi * 5));
            //Obstacle on negative part of the lane
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(-0.1f, 10) }, 1, 1, 1).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.GreaterThan(Constants.Pi * 5));
            //Obstacle is exactly on the border of lane
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(0, 11) }, 1, 1, 1).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.EqualTo(Constants.Pi * 5));
            //Obstacle on positive part of the lane, but has negative Y
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(1.1f, -0.05f) }, 1, 1, 1).
                DistancesToObstaclesOnCircle(cmm).Single(), Is.GreaterThan(0));
            //Curve down
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(0, -10) }, 1, 1, 1).
                DistancesToObstaclesOnCircle(new CircleMotionModel(new Velocity(5, -1))).Single(), Is.EqualTo(Constants.Pi * 5));
            //Two obstacles are exactly on X axis
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(0, 9), new Vector2(0, 11) }, 1, 1, 1).
                DistancesToObstaclesOnCircle(cmm), Is.EquivalentTo(new[] {Constants.Pi * 5, Constants.Pi * 5}));
            //Turning almost on place, there is no circle with (R - Rrobot) radius
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(0, 1.5f) }, 1, 1, 1).
                DistancesToObstaclesOnCircle(new CircleMotionModel(new Velocity(0.5, 1))).Single(), Is.EqualTo(Constants.Pi * 0.5));
        }

        [Test]
        public void GetDistanceToClosestObstacle()
        {
            //One obstacle staright ahead, moving on line
            Assert.That(double.IsPositiveInfinity(new ObstaclesOnLaneDistanceCalculator(obstacles: new Vector2[0], robotRadius: 1d, linearDecelerationMax: 1d, maxRange: 1d).
                GetDistanceToClosestObstacle(new Velocity(50, 0))));

            //One obstacle staright ahead, moving on line
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(3, 0) }, 1, 1, 1).
                GetDistanceToClosestObstacle(new Velocity(50, 0)), Is.EqualTo(2));

            //Obstacle is exactly on X axis, moving on circle
            Assert.That(new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(0, 10) }, 1, 1, 1).
                GetDistanceToClosestObstacle(new Velocity(50, 10)), Is.EqualTo(Constants.Pi * 5 - 1));
        }

        [Test]
        public void IsVelocityAdmissible()
        {
            var dc = new ObstaclesOnLaneDistanceCalculator(new Vector2[0], 1, 0.5, 1);

            Assert.That(dc.IsVelocityAdmissible(new Velocity(1, 0), 2));
            Assert.That(dc.IsVelocityAdmissible(new Velocity(1, 1000), 2));
            Assert.That(dc.IsVelocityAdmissible(new Velocity(Constants.Sqrt2, 0), 2));
            Assert.That(dc.IsVelocityAdmissible(new Velocity(2, 0), 2), Is.False);
            Assert.That(dc.IsVelocityAdmissible(new Velocity(2, 1000), 2), Is.False);
        }

        [Test]
        public void Evaluate()
        {
            var dc = new ObstaclesOnLaneDistanceCalculator(new[] { new Vector2(3, 0), new Vector2(0, 6) }, 1, 0.5, 5);

            //Rotation on place
            Assert.That(dc.Evaluate(new Velocity(0, 10)), Is.EqualTo(1));
            //Right into the obstacle on high speed
            Assert.That(dc.Evaluate(new Velocity(10, 0)), Is.EqualTo(0));
            //Into the obstacle that is on the brink of rangefinder perception and on the longest trajectory
            Assert.That(dc.Evaluate(new Velocity((5d + 1) / 2 / 10, 1d / 10)), Is.EqualTo(0.5).Within(1e-7));
            //Right into the obstacle on not so high speed
            Assert.That(dc.Evaluate(new Velocity(1, 0)), Is.GreaterThan(0).And.LessThan(0.5));
        }
    }
}