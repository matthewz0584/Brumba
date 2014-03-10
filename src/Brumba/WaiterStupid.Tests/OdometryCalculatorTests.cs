using System;
using Brumba.Utils;
using Brumba.WaiterStupid.Odometry;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
	[TestFixture]
    public class OdometryCalculatorTests
    {
		private DiffDriveOdometryCalculator _ddoc;

		[SetUp]
		public void SetUp()
		{
		    _ddoc = new DiffDriveOdometryCalculator(new DiffDriveOdometryConstants
		                            {WheelBase = 4, WheelRadius = 1, TicksPerRotation = 5});
		}

	    [Test]
        public void CalculatePoseDelta()
	    {
	        Assert.That(_ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: 10, rightTicks: 10).
                EqualsRelatively(new Vector3(2 * MathHelper.TwoPi, 0, 0), 1e-5));

	        Assert.That(_ddoc.CalculatePoseDelta(oldTheta: MathHelper.PiOver2, leftTicks: 10, rightTicks: 10).
                EqualsRelatively(new Vector3(0, 2 * MathHelper.TwoPi, 0), 1e-5));

            Assert.That(_ddoc.CalculatePoseDelta(oldTheta: MathHelper.PiOver4, leftTicks: 10, rightTicks: 10).
                EqualsRelatively(new Vector3((float)Math.Sqrt(2) * MathHelper.TwoPi, (float)Math.Sqrt(2) * MathHelper.TwoPi, 0), 1e-5));

            Assert.That(_ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: -10, rightTicks: -10).
                EqualsRelatively(new Vector3(-2 * MathHelper.TwoPi, 0, 0), 1e-5));

	        Assert.That(_ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: 5, rightTicks: -5).
                EqualsRelatively(new Vector3(0, 0, -MathHelper.Pi), 1e-5));

            Assert.That(_ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: 10, rightTicks: -10).
                EqualsRelatively(new Vector3(0, 0, -MathHelper.TwoPi), 1e-5));

            Assert.That(_ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: 0, rightTicks: 5).
                EqualsRelatively(new Vector3(MathHelper.Pi, 0, MathHelper.PiOver2), 1e-5));
	    }

	    [Test]
	    public void UpdateOdometry()
	    {
	        var prevOdometry = new DiffDriveOdometryState {LeftTicks = 5, RightTicks = 10, Pose = new Vector3(10, 0, 0)};

	        var nextOdometry = _ddoc.UpdateOdometry(prevOdometry, 15, 20);

            Assert.That(nextOdometry.LeftTicks, Is.EqualTo(15));
            Assert.That(nextOdometry.RightTicks, Is.EqualTo(20));
            Assert.That(nextOdometry.Pose, Is.EqualTo(new Vector3(10 + 2 * MathHelper2.TwoPi, 0, 0)));
	    }
    }
}
