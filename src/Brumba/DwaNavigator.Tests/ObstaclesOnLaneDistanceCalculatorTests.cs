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
            var dc = new ObstaclesOnLaneDistanceCalculator(1);

            //No obstacles
            Assert.That(dc.DistancesToObstaclesOnLine(obstacles: new Vector2[0]), Is.Empty);

            //Obstacles are not in the lane
            Assert.That(dc.DistancesToObstaclesOnLine(new[] { new Vector2(0, 10), new Vector2(0, -10), new Vector2(-3, 0) }), Is.Empty);

            //One obstacle staright ahead
            Assert.That(dc.DistancesToObstaclesOnLine(new[] { new Vector2(3, 0) }).Single(), Is.EqualTo(3));

            //One obstacle ahead and to the side
            Assert.That(dc.DistancesToObstaclesOnLine(new[] { new Vector2(3, 0.5f) }).Single(), Is.EqualTo(3));

            //Obstacle is exactly on the border of the lane
            Assert.That(dc.DistancesToObstaclesOnLine(new[] { new Vector2(3, 1) }).Single(), Is.EqualTo(3));
            
            //Two obstacles ahead, one is closer
            Assert.That(dc.DistancesToObstaclesOnLine(new[] { new Vector2(3, 0.5f), new Vector2(4, -0.5f) }), Is.EquivalentTo(new [] {3d, 4}));
        }

        [Test]
        public void DistancesToObstaclesOnCircle()
        {
            var dc = new ObstaclesOnLaneDistanceCalculator(1);
            var cmm = new CircleMotionModel(new Velocity(5, 1));

            //No obstacles
            
            Assert.That(dc.DistancesToObstaclesOnCircle(cmm, obstacles: new Vector2[0]), Is.Empty);

            //Obstacle is not on the lane
            Assert.That(dc.DistancesToObstaclesOnCircle(cmm, new[] { new Vector2(10, 10) }), Is.Empty);

            //Obstacle is exactly on X axis
            Assert.That(dc.DistancesToObstaclesOnCircle(cmm, new[] { new Vector2(0, 10) }).Single(), Is.EqualTo(Constants.Pi * 5));
            //Obstacle on positive part of the lane
            Assert.That(dc.DistancesToObstaclesOnCircle(cmm, new[] { new Vector2(0.1f, 10) }).Single(), Is.LessThan(Constants.Pi * 5));
            //Obstacle on negative part of the lane
            Assert.That(dc.DistancesToObstaclesOnCircle(cmm, new[] { new Vector2(-0.1f, 10) }).Single(), Is.GreaterThan(Constants.Pi * 5));
            //Obstacle is exactly on the border of lane
            Assert.That(dc.DistancesToObstaclesOnCircle(cmm, new[] { new Vector2(0, 11) }).Single(), Is.EqualTo(Constants.Pi * 5));
            //Obstacle on positive part of the lane, but has negative Y
            Assert.That(dc.DistancesToObstaclesOnCircle(cmm, new[] { new Vector2(1.1f, -0.05f) }).Single(), Is.GreaterThan(0));
            //Curve down
            Assert.That(dc.DistancesToObstaclesOnCircle(new CircleMotionModel(new Velocity(5, -1)), new[] { new Vector2(0, -10) }).Single(), Is.EqualTo(Constants.Pi * 5));
            //Two obstacles are exactly on X axis
            Assert.That(dc.DistancesToObstaclesOnCircle(cmm, new[] {new Vector2(0, 9), new Vector2(0, 11)}),
                Is.EquivalentTo(new[] {Constants.Pi * 5, Constants.Pi * 5}));
        }

        [Test]
        public void GetDistanceToClosestObstacle()
        {
            var dc = new ObstaclesOnLaneDistanceCalculator(robotRadius: 1d);

            //One obstacle staright ahead, moving on line
            Assert.That(double.IsPositiveInfinity(dc.GetDistanceToClosestObstacle(new Velocity(50, 0), new Vector2[0])));

            //One obstacle staright ahead, moving on line
            Assert.That(dc.GetDistanceToClosestObstacle(new Velocity(50, 0), new[] { new Vector2(3, 0) }), Is.EqualTo(2));

            //Obstacle is exactly on X axis, moving on circle
            Assert.That(dc.GetDistanceToClosestObstacle(new Velocity(50, 10), new[] { new Vector2(0, 10) }), Is.EqualTo(Constants.Pi * 5 - 1));
        }
    }
}