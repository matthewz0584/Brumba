using System;
using Brumba.Utils;
using Brumba.WaiterStupid;
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
                wheelAngularAccelerationMax: 4,
                wheelAngularVelocityMax: 13, // 1m/s
                wheelRadius: 0.076,
                wheelBase: 0.3,
                robotRadius: 0.3,
                rangefinderMaxRange: 10,
                dt: 0.25);
        }

        [Test]
        public void AccelerationMax()
        {
            Assert.That(_dwan.AccelerationMax, Is.EqualTo(new Velocity(0.076d / 2 * (4 + 4), 0.076d / 0.3d * (4 + 4))));
        }

        [Test]
        public void VelocityMax()
        {
            Assert.That(_dwan.VelocityMax, Is.EqualTo(new Velocity(0.076d / 2 * (13 + 13), 0.076d / 0.3d * (13 + 13))));
        }

        [Test]
        public void ClearStraightPath()
        {
            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(), 0),
                        new Vector2(10, 0),
                        new Vector2[0]);
            Assert.That(_dwan.OptimalVelocity.Velocity, Is.EqualTo(new Velocity(_dwan.AccelerationMax.Linear * 0.25, 0)));
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));
        }

        [Test]
        public void ObstacleStraightAhead()
        {
            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(), 0),
                        new Vector2(10, 0),
                        new[]
                        {
                            new Vector2(2.5f, 0)
                        });

            //Console.WriteLine(_dwan.VelocitiesEvaluation.ToString(21, 21));
            //Evading obstacle by choosing the slightest curve
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration.EqualsWithError(new Vector2(0.9f, 1), 1e-7));
        }

        [Test]
        public void ObstacleStraightAheadOnHighSpeed()
        {
            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(1.05f, 0), 0),
                        new Vector2(10, 0),
                        new[]
                        {
                            new Vector2(2.5f, 0)
                        });

            Console.WriteLine(_dwan.VelocitiesEvaluation.ToString(21, 21));
            //Speed is to high, no options of circumventing the obstacle, decelerating
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(-1, -1)));
        }

        [Test]
        public void ObstacleStraightAheadOnCloseDistance()
        {
            _dwan.Update(new Pose(new Vector2(), 0),
                        new Pose(new Vector2(), 0),
                        new Vector2(10, 0),
                        new[]
                        {
                            new Vector2(0.4f, 0)
                        });

            //Evading obstacle by turning on place
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(0.2f, -0.2f)));

            _dwan.Update(new Pose(new Vector2(), 0),
            new Pose(new Vector2(), 0),
            new Vector2(10, 0),
            new[]
                        {
                            new Vector2(0.301f, 0)
                        });

            //Interesting effect: obstacle is so close that every velocity with linear component is severely penalized.
            //That penalty propagates to entirely rotational trajectories except (1, -1), which lays on the border of matrix
            //and does not get smoothed. Hence, in limit (1, -1) is chosen, which does not seem very correct.
            Assert.That(_dwan.OptimalVelocity.WheelAcceleration, Is.EqualTo(new Vector2(1, -1)));
        }
    }
}