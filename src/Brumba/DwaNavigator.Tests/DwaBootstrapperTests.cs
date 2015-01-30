using System;
using Brumba.Common;
using Brumba.McLrfLocalizer;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class DwaBootstrapperTests
    {
        private DwaBootstrapper _dwab;

        [SetUp]
        public void SetUp()
        {
            _dwab = new DwaBootstrapper(
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
                laneWidthCoef: 1,
                dt: 0.25);
        }

        [Test]
        public void ClearStraightPath()
        {
            _dwab.Update(new Pose(new Vector2(), 0),
                        new Velocity(), 
                        new Vector2(10, 0),
                        new float[0]);
            Assert.That(_dwab.OptimalVelocity.Velocity.Linear, Is.GreaterThan(0));
            Assert.That(_dwab.OptimalVelocity.Velocity.Angular, Is.EqualTo(0));
            Assert.That(_dwab.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));
        }

        [Test]
        public void ObstacleStraightAhead()
        {
            _dwab.Update(new Pose(new Vector2(), 0),
                        new Velocity(), 
                        new Vector2(10, 0),
                        new[] { 10, 2.5f });

            //Console.WriteLine(_dwab.VelocitiesEvaluation.ToString(21, 21));
            //Evading obstacle by choosing the slightest curve
            Assert.That(_dwab.OptimalVelocity.WheelAcceleration.EqualsWithError(new Vector2(0.9f, 1), 1e-7));
        }

        [Test]
        public void WallStraightAhead()
        {
            _dwab = new DwaBootstrapper(
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
                laneWidthCoef: 1,
                dt: 0.25);

            _dwab.Update(new Pose(new Vector2(), 0),
                        new Velocity(1f, 0),
                        new Vector2(10, 0),
                        new[] { 1.65f, 1.6f, 1.55f, 1.5f, 1.5f, 1.5f, 1.55f, 1.6f, 8.6f });

            //Wall is large, evading by the sharp turn
            var wheelAcceleration = _dwab.OptimalVelocity.WheelAcceleration;
            Assert.That(wheelAcceleration, Is.EqualTo(new Vector2(0.4f, 1)));

            _dwab.Update(new Pose(new Vector2(), 0),
                        new Velocity(1.5f, 0),
                        new Vector2(10, 0),
                        new[] { 1.65f, 1.6f, 1.55f, 1.5f, 1.5f, 1.5f, 1.55f, 1.6f, 8.6f });

            //Velocity is even higher, evading by sharper turn
            Assert.That(_dwab.OptimalVelocity.WheelAcceleration.X, Is.LessThan(wheelAcceleration.X));
            Assert.That(_dwab.OptimalVelocity.WheelAcceleration.Y, Is.GreaterThanOrEqualTo(wheelAcceleration.Y));
        }

        [Test]
        public void ObstacleStraightAheadOnCloseDistance()
        {
            _dwab.Update(new Pose(new Vector2(), 0),
                        new Velocity(), 
                        new Vector2(10, 0),
                        new[] { 10, 0.31f });

            //Evading obstacle by turning on place
            Console.WriteLine(_dwab.VelocitiesEvaluation.ToString(7, 7));
            Assert.That(_dwab.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(-0.9f, 1)));

            _dwab.Update(new Pose(new Vector2(), 0),
                        new Velocity(), 
                        new Vector2(10, 0),
                        new[] { 10, 0.301f });

            //Interesting effect: obstacle is so close that every velocity with linear component is severely penalized.
            //That penalty propagates to entirely rotational trajectories except (1, -1), which lays on the border of matrix
            //and does not get smoothed. Hence, in limit (1, -1) is chosen, which does not seem very correct.
            Assert.That(_dwab.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, -1)).Or.EqualTo(new Vector2(-1, 1)));
        }

        [Test]
        public void MaxRangesAreFilteredOut()
        {
            _dwab = new DwaBootstrapper(9.1, 0.209, 0.076, 0.3, 0.3, 1.5, 1, 1, 0.03, new RangefinderProperties
                    {
                        AngularRange = Constants.Grad * 40,
                        AngularResolution = Constants.Grad * 5,
                        OriginPose = new Pose(),
                        MaxRange = 11
                    }, 1, 0.25);

            _dwab.Update(new Pose(new Vector2(), 0),
                        new Velocity(0.9f, 0),
                        new Vector2(10, 0),
                        new[] { 10, 10, 10, 10, 10, 10, 10, 10, 10f });

            //Wall is large, evading by the sharpest turn
            Assert.That(_dwab.OptimalVelocity.WheelAcceleration, Is.Not.EqualTo(new Vector2(1, 1)));

            _dwab = new DwaBootstrapper(9.1, 0.209, 0.076, 0.3, 0.3, 1.5, 1, 1, 0.03, new RangefinderProperties
                    {
                        AngularRange = Constants.Grad * 40,
                        AngularResolution = Constants.Grad * 5,
                        OriginPose = new Pose(),
                        MaxRange = 10
                    }, 1, 0.25);

            _dwab.Update(new Pose(new Vector2(), 0),
                        new Velocity(0.9f, 0),
                        new Vector2(10, 0),
                        new[] { 10, 10, 10, 10, 10, 10, 10, 10, 10f });

            Console.WriteLine(_dwab.VelocitiesEvaluation.ToString(21, 21));
            Console.WriteLine(_dwab.OptimalVelocity.WheelAcceleration);
            Console.WriteLine(_dwab.OptimalVelocity.Velocity);
            //Wall is large, evading by the sharpest turn
            Assert.That(_dwab.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));
        }

        [Test]
        public void LessThanMinRangesAreFilteredOut()
        {
            _dwab.Update(new Pose(new Vector2(), 0),
                        new Velocity(), 
                        new Vector2(10, 0),
                        new [] {0.1f, 0.1f, 0.1f});
            Assert.That(_dwab.OptimalVelocity.Velocity.Linear, Is.GreaterThan(0));
            Assert.That(_dwab.OptimalVelocity.Velocity.Angular, Is.EqualTo(0));
            Assert.That(_dwab.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));
        }
    }
}