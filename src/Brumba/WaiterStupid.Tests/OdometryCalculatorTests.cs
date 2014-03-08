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
		private OdometryCalculator _odometryCalc;

		[SetUp]
		public void SetUp()
		{
			_odometryCalc = new OdometryCalculator { Constants = { WheelBase = 1, WheelRadius = 1 } };
		}

		[Test]
		public void TicksToAngularVelocity()
		{
			float deltaT = 2;
		    _odometryCalc.Constants.TicksPerRotation = 5;
			Assert.That(_odometryCalc.TicksToAngularVelocity(10, deltaT), Is.EqualTo(5 * MathHelper.TwoPi / 5).Within(1e-6));
		}

		[Test]
		public void Velocity()
		{
			float omegaR = 0;
			float omegaL = 0;
			float theta = 0;
			Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3()));

			omegaR = 1;
			omegaL = 1;
			theta = 0;
			Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(1, 0, 0)));

			omegaR = -1;
			omegaL = -1;
			theta = 0;
			Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(-1, 0, 0)));

			omegaR = 1;
			omegaL = -1;
			theta = 0;
			Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(0, 0, 2)));

			omegaR = 1;
			omegaL = 1;
			theta = MathHelper.PiOver4;
			Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(1 / (float)Math.Sqrt(2), 1 / (float)Math.Sqrt(2), 0)));
		}

		[Test]
		public void Pose()
		{
			var oldPose = new Vector3();
			var velocity = new Vector3();
			var deltaT = 0.1f;
			Assert.That(_odometryCalc.UpdatePose(oldPose, velocity, deltaT), Is.EqualTo(new Vector3()));

			oldPose = new Vector3();
			velocity = new Vector3(1, 1, 0);
			Assert.That(_odometryCalc.UpdatePose(oldPose, velocity, deltaT), Is.EqualTo(new Vector3(0.1f, 0.1f, 0)));

			oldPose = new Vector3();
			velocity = new Vector3(0, 0, 1);
			Assert.That(_odometryCalc.UpdatePose(oldPose, velocity, deltaT), Is.EqualTo(new Vector3(0, 0, 0.1f)));

			velocity = new Vector3(0, 0, 1);
			Assert.That(_odometryCalc.UpdatePose(oldPose, velocity, 10), Is.EqualTo(new Vector3(0, 0, 10f)));
		}

		[Test]
		public void Acceptance()
		{
			_odometryCalc.Constants.TicksPerRotation = 2;
			_odometryCalc.Constants.WheelRadius = 1 / MathHelper.Pi;

		    var newOdometry = _odometryCalc.UpdateOdometry(
                previousOdometry: new OdometryState { LeftTicks = 2, RightTicks = 2 },
                deltaT: 0.1f, leftTicks: 2, rightTicks: 2);

			Assert.That(newOdometry.LeftTicks, Is.EqualTo(2));
			Assert.That(newOdometry.RightTicks, Is.EqualTo(2));
			Assert.That(newOdometry.Velocity, Is.EqualTo(new Vector3()));
			Assert.That(newOdometry.Pose, Is.EqualTo(new Vector3()));
            Assert.That(newOdometry.PoseDelta, Is.EqualTo(new Vector3()));

			newOdometry = _odometryCalc.UpdateOdometry(
                previousOdometry: new OdometryState { Pose = new Vector3(1, 0, 0), LeftTicks = 0, RightTicks = 0 },
                deltaT: 0.1f, leftTicks: 2, rightTicks: 2);

			Assert.That(newOdometry.LeftTicks, Is.EqualTo(2));
			Assert.That(newOdometry.RightTicks, Is.EqualTo(2));
			Assert.That(newOdometry.Velocity, Is.EqualTo(new Vector3(20, 0, 0)));
			Assert.That(newOdometry.Pose, Is.EqualTo(new Vector3(1, 0, 0) + 0.5f * new Vector3(20, 0, 0) * 0.1f));
            Assert.That(newOdometry.PoseDelta, Is.EqualTo(0.5f * new Vector3(20, 0, 0) * 0.1f));
		}

	    [Test]
	    public void PoseDelta()
	    {
            _odometryCalc.Constants.TicksPerRotation = 5;
            _odometryCalc.Constants.WheelBase = 4;

	        Assert.That(_odometryCalc.CalculatePoseDelta(oldTheta: 0, leftTicks: 10, rightTicks: 10).
                EqualsRelatively(new Vector3(2 * MathHelper.TwoPi, 0, 0), 1e-5));

	        Assert.That(_odometryCalc.CalculatePoseDelta(oldTheta: MathHelper.PiOver2, leftTicks: 10, rightTicks: 10).
                EqualsRelatively(new Vector3(0, 2 * MathHelper.TwoPi, 0), 1e-5));

            Assert.That(_odometryCalc.CalculatePoseDelta(oldTheta: MathHelper.PiOver4, leftTicks: 10, rightTicks: 10).
                EqualsRelatively(new Vector3((float)Math.Sqrt(2) * MathHelper.TwoPi, (float)Math.Sqrt(2) * MathHelper.TwoPi, 0), 1e-5));

            Assert.That(_odometryCalc.CalculatePoseDelta(oldTheta: 0, leftTicks: -10, rightTicks: -10).
                EqualsRelatively(new Vector3(-2 * MathHelper.TwoPi, 0, 0), 1e-5));

	        Assert.That(_odometryCalc.CalculatePoseDelta(oldTheta: 0, leftTicks: 5, rightTicks: -5).
                EqualsRelatively(new Vector3(0, 0, -MathHelper.Pi), 1e-5));

            Assert.That(_odometryCalc.CalculatePoseDelta(oldTheta: 0, leftTicks: 10, rightTicks: -10).
                EqualsRelatively(new Vector3(0, 0, -MathHelper.TwoPi), 1e-5));

            Assert.That(_odometryCalc.CalculatePoseDelta(oldTheta: 0, leftTicks: 0, rightTicks: 5).
                EqualsRelatively(new Vector3(MathHelper.Pi, 0, MathHelper.PiOver2), 1e-5));
	    }
    }
}
