using System;
using Brumba.Common;
using Brumba.Utils;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DiffDriveOdometry.Tests
{
	[TestFixture]
    public class DiffDriveOdometryCalculatorTests
    {
		private DiffDriveOdometryCalculator _ddoc;

		[SetUp]
		public void SetUp()
		{
		    _ddoc = new DiffDriveOdometryCalculator(wheelRadius: 1, wheelBase: 4, ticksPerRotation: 5);
		}

        [Test]
        public void WheelToRobotKinematics()
        {
            Assert.That(_ddoc.WheelsToRobotKinematics(new Vector2(0, 0)), Is.EqualTo(new Velocity(0, 0)));

            Assert.That(_ddoc.WheelsToRobotKinematics(new Vector2(MathHelper.TwoPi, MathHelper.TwoPi)), Is.EqualTo(new Velocity(MathHelper.TwoPi, 0)));

            Assert.That(_ddoc.WheelsToRobotKinematics(new Vector2(-1, -1)), Is.EqualTo(new Velocity(-1, 0)));

            Assert.That(_ddoc.WheelsToRobotKinematics(new Vector2(1, -1)), Is.EqualTo(new Velocity(0, -0.5)));

            Assert.That(_ddoc.WheelsToRobotKinematics(new Vector2(1, 1)), Is.EqualTo(new Velocity(1, 0)));

            Assert.That(_ddoc.WheelsToRobotKinematics(new Vector2(1, 0)), Is.EqualTo(new Velocity(0.5f, -0.25)));
        }

        [Test]
        public void CalculatePoseDelta()
        {
            var pd1 = _ddoc.CalculatePoseDelta(oldTheta: 0, leftTicksDelta: 10, rightTicksDelta: 10);
            Assert.That(pd1.Position.EqualsRelatively(new Vector2(2 * MathHelper.TwoPi, 0), 1e-5));
            Assert.That(pd1.Bearing, Is.EqualTo(0));

            var pd2 = _ddoc.CalculatePoseDelta(oldTheta: MathHelper.PiOver2, leftTicksDelta: 10, rightTicksDelta: 10);
            Assert.That(pd2.Position.EqualsRelatively(new Vector2(0, 2 * MathHelper.TwoPi), 1e-5));
            Assert.That(pd2.Bearing, Is.EqualTo(0));

            var pd3 = _ddoc.CalculatePoseDelta(oldTheta: MathHelper.PiOver4, leftTicksDelta: 10, rightTicksDelta: 10);
            Assert.That(pd3.Position.
                EqualsRelatively(new Vector2((float)Math.Sqrt(2) * MathHelper.TwoPi, (float)Math.Sqrt(2) * MathHelper.TwoPi), 1e-5));
            Assert.That(pd3.Bearing, Is.EqualTo(0));

            var pd5 = _ddoc.CalculatePoseDelta(oldTheta: 0, leftTicksDelta: 5, rightTicksDelta: -5);
            Assert.That(pd5.Position.EqualsRelatively(new Vector2(0, 0), 1e-5));
            Assert.That(pd5.Bearing, Is.EqualTo(-MathHelper.Pi));
        }

	    [Test]
	    public void CalculateVelocity()
	    {
            Assert.That(_ddoc.CalculateVelocity(14, 14, 1).Linear, Is.EqualTo(14d / 5 * MathHelper.TwoPi).Within(1e-6));
            Assert.That(_ddoc.CalculateVelocity(14, 14, 1).Angular, Is.EqualTo(0));

            Assert.That(_ddoc.CalculateVelocity(14, 14, 1).Linear, Is.EqualTo(2 * _ddoc.CalculateVelocity(14, 14, 2).Linear).Within(1e-6));
	    }

	    [Test]
	    public void UpdateOdometry()
	    {
            var od = _ddoc.UpdateOdometry(new Pose(new Vector2(10, 5), MathHelper.PiOver2), 10, 10, 2.0);

            Assert.That(od.Item1.Position.EqualsWithError(new Vector2(10, 5 + 2 * MathHelper.TwoPi), 1e-5));
            Assert.That(od.Item1.Bearing, Is.EqualTo(MathHelper.PiOver2));
            Assert.That(od.Item2, Is.EqualTo(new Velocity(MathHelper.TwoPi, 0)));
	    }

        [Test]
        public void RobotToWheelsKinematics()
        {
            Assert.That(_ddoc.RobotKinematicsToWheels(new Velocity(0, 0)), Is.EqualTo(new Vector2(0, 0)));

            Assert.That(_ddoc.RobotKinematicsToWheels(new Velocity(MathHelper.TwoPi, 0)), Is.EqualTo(new Vector2(MathHelper.TwoPi, MathHelper.TwoPi)));

            Assert.That(_ddoc.RobotKinematicsToWheels(new Velocity(-1, 0)), Is.EqualTo(new Vector2(-1, -1)));

            Assert.That(_ddoc.RobotKinematicsToWheels(new Velocity(0, -0.5)), Is.EqualTo(new Vector2(1, -1)));

            Assert.That(_ddoc.RobotKinematicsToWheels(new Velocity(1, 0)), Is.EqualTo(new Vector2(1, 1)));

            Assert.That(_ddoc.RobotKinematicsToWheels(new Velocity(0.5f, -0.25)), Is.EqualTo(new Vector2(1, 0)));
        }   
    }
}
