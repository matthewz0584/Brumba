using System;
using Brumba.McLrfLocalizer;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class DwaNavigatorTests
    {
        private DwaNavigator _dwan;

        [SetUp]
        public void SetUp()
        {
            _dwan = new DwaNavigator(
                robotMass: 9.1,
                robotInertiaMoment: 0.209,
                wheelRadius: 0.076,
                wheelBase: 0.3,
                robotRadius: 0.3,
                velocityMax: 1.5,
                breakageDeceleration: 1,
                currentToTorque: 1,
                frictionTorque: 0.03,
                rangefinderProperties: new RangefinderProperties
                {
                    AngularRange = Constants.Pi,
                    AngularResolution = Constants.PiOver2,
                    MaxRange = 10,
                    OriginPose = new Pose()
                },
                dt: 0.25);
        }

        //[Test]
        //public void AccelerationMax()
        //{
        //    Assert.That(_dwan.AccelerationMax, Is.EqualTo(new Velocity(0.076d / 2 * (4 + 4), 0.076d / 0.3d * (4 + 4))));
        //}

        [Test]
        public void VelocityMax()
        {
            Assert.That(_dwan.VelocityMax, Is.EqualTo(new Velocity(1.5, 0.076d / 0.3d * (1.5 + 1.5) / 0.076)));
        }

        [Test]
        public void ClearStraightPath()
        {
            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(), 0),
                        new Vector2(10, 0),
                        new float[0]);
            Assert.That(_dwan.OptimalVelocity.Velocity.Linear, Is.GreaterThan(0));
            Assert.That(_dwan.OptimalVelocity.Velocity.Angular, Is.EqualTo(0));
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));
        }

        [Test]
        public void ObstacleStraightAhead()
        {
            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(), 0),
                        new Vector2(10, 0),
                        new[] { 10, 2.5f });

            //Console.WriteLine(_dwan.VelocitiesEvaluation.ToString(21, 21));
            //Evading obstacle by choosing the slightest curve
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration.EqualsWithError(new Vector2(0.9f, 1), 1e-7));
        }

        [Test]
        public void WallStraightAhead()
        {
            _dwan = new DwaNavigator(
                robotMass: 9.1,
                robotInertiaMoment: 0.209,
                wheelRadius: 0.076,
                wheelBase: 0.3,
                robotRadius: 0.3,
                velocityMax: 1.5,
                breakageDeceleration: 1,
                currentToTorque: 1,
                frictionTorque: 0.03,
                rangefinderProperties: new RangefinderProperties
                {
                    AngularRange = Constants.Grad * 40, AngularResolution = Constants.Grad * 5, MaxRange = 10, OriginPose = new Pose()
                },
                dt: 0.25);

            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(1f, 0), 0),
                        new Vector2(10, 0),
                        new[] { 1.65f, 1.6f, 1.55f, 1.5f, 1.5f, 1.5f, 1.55f, 1.6f, 8.6f });

            //Wall is large, evading by the sharp turn
            var wheelAcceleration = _dwan.OptimalVelocity.WheelAcceleration;
            Assert.That(wheelAcceleration, Is.EqualTo(new Vector2(0.4f, 1)));

            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(1.5f, 0), 0),
                        new Vector2(10, 0),
                        new[] { 1.65f, 1.6f, 1.55f, 1.5f, 1.5f, 1.5f, 1.55f, 1.6f, 8.6f });

            //Velocity is even higher, evading by sharper turn
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration.X, Is.LessThan(wheelAcceleration.X));
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration.Y, Is.GreaterThanOrEqualTo(wheelAcceleration.Y));
        }

        [Test]
        public void ObstacleStraightAheadOnCloseDistance()
        {
            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(), 0),
                        new Vector2(10, 0),
                        new[] { 10, 0.31f });

            //Evading obstacle by turning on place
            Console.WriteLine(_dwan.VelocitiesEvaluation.ToString(7, 7));
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(0.2f, -0.2f)).Or.EqualTo(new Vector2(-0.2f, 0.2f)));

            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(), 0),
                        new Vector2(10, 0),
                        new[] { 10, 0.301f });

            //Interesting effect: obstacle is so close that every velocity with linear component is severely penalized.
            //That penalty propagates to entirely rotational trajectories except (1, -1), which lays on the border of matrix
            //and does not get smoothed. Hence, in limit (1, -1) is chosen, which does not seem very correct.
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, -1)).Or.EqualTo(new Vector2(-1, 1)));
        }

        [Test]
        public void MaxRangesAreFilteredOut()
        {
            _dwan = new DwaNavigator(9.1, 0.209, 0.076, 0.3, 0.3, 1.5, 1, 1, 0.03, new RangefinderProperties
                    {
                        AngularRange = Constants.Grad * 40,
                        AngularResolution = Constants.Grad * 5,
                        OriginPose = new Pose(),
                        MaxRange = 11
                    }, 0.25);

            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(0.9f, 0), 0),
                        new Vector2(10, 0),
                        new[] { 10, 10, 10, 10, 10, 10, 10, 10, 10f });

            //Wall is large, evading by the sharpest turn
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.Not.EqualTo(new Vector2(1, 1)));

            _dwan = new DwaNavigator(9.1, 0.209, 0.076, 0.3, 0.3, 1.5, 1, 1, 0.03, new RangefinderProperties
                    {
                        AngularRange = Constants.Grad * 40,
                        AngularResolution = Constants.Grad * 5,
                        OriginPose = new Pose(),
                        MaxRange = 10
                    }, 0.25);

            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(0.9f, 0), 0),
                        new Vector2(10, 0),
                        new[] { 10, 10, 10, 10, 10, 10, 10, 10, 10f });

            Console.WriteLine(_dwan.VelocitiesEvaluation.ToString(21, 21));
            Console.WriteLine(_dwan.OptimalVelocity.WheelAcceleration);
            Console.WriteLine(_dwan.OptimalVelocity.Velocity);
            //Wall is large, evading by the sharpest turn
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));
        }

        [Test]
        public void LessThanMinRangesAreFilteredOut()
        {
            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(), 0),
                        new Vector2(10, 0),
                        new [] {0.1f, 0.1f, 0.1f});
            Assert.That(_dwan.OptimalVelocity.Velocity.Linear, Is.GreaterThan(0));
            Assert.That(_dwan.OptimalVelocity.Velocity.Angular, Is.EqualTo(0));
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));
        }
    }
}