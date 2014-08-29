﻿using System;
using Brumba.Utils;
using Brumba.WaiterStupid;
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
		    _ddoc = new DiffDriveOdometryCalculator(new DiffDriveOdometryConstants
		                            {WheelBase = 4, WheelRadius = 1, TicksPerRotation = 5});
		}

	    [Test]
        public void CalculatePoseDelta()
	    {
		    var pd1 = _ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: 10, rightTicks: 10);
		    Assert.That(pd1.Position.EqualsRelatively(new Vector2(2 * MathHelper.TwoPi, 0), 1e-5));
			Assert.That(pd1.Bearing, Is.EqualTo(0));

		    var pd2 = _ddoc.CalculatePoseDelta(oldTheta: MathHelper.PiOver2, leftTicks: 10, rightTicks: 10);
		    Assert.That(pd2.Position.EqualsRelatively(new Vector2(0, 2 * MathHelper.TwoPi), 1e-5));
			Assert.That(pd2.Bearing, Is.EqualTo(0));

		    var pd3 = _ddoc.CalculatePoseDelta(oldTheta: MathHelper.PiOver4, leftTicks: 10, rightTicks: 10);
			Assert.That(pd3.Position.
				EqualsRelatively(new Vector2((float)Math.Sqrt(2) * MathHelper.TwoPi, (float)Math.Sqrt(2) * MathHelper.TwoPi), 1e-5));
			Assert.That(pd3.Bearing, Is.EqualTo(0));

		    var pd4 = _ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: -10, rightTicks: -10);
			Assert.That(pd4.Position.EqualsRelatively(new Vector2(-2 * MathHelper.TwoPi, 0), 1e-5));
			Assert.That(pd4.Bearing, Is.EqualTo(0));

		    var pd5 = _ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: 5, rightTicks: -5);
			Assert.That(pd5.Position.EqualsRelatively(new Vector2(0, 0), 1e-5));
			Assert.That(pd5.Bearing, Is.EqualTo(-MathHelper.Pi));

		    var pd6 = _ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: 10, rightTicks: -10);
			Assert.That(pd6.Position.EqualsRelatively(new Vector2(0, 0), 1e-5));
			Assert.That(pd6.Bearing, Is.EqualTo(-MathHelper.TwoPi));

		    var pd7 = _ddoc.CalculatePoseDelta(oldTheta: 0, leftTicks: 0, rightTicks: 5);
			Assert.That(pd7.Position.EqualsRelatively(new Vector2(MathHelper.Pi, 0), 1e-5));
			Assert.That(pd7.Bearing, Is.EqualTo(MathHelper.PiOver2));
	    }

	    [Test]
	    public void UpdateOdometry()
	    {
	        var prevOdometry = new DiffDriveOdometryState {LeftTicks = 5, RightTicks = 10, Pose = new Pose(new Vector2(10, 0), 0)};

	        var nextOdometry = _ddoc.UpdateOdometry(prevOdometry, 15, 20);

            Assert.That(nextOdometry.LeftTicks, Is.EqualTo(15));
            Assert.That(nextOdometry.RightTicks, Is.EqualTo(20));
            Assert.That(nextOdometry.Pose, Is.EqualTo(new Pose(new Vector2(10 + 2 * MathHelper.TwoPi, 0), 0)));
	    }

        //[Test]
        //public void TicksToAngularVelocity()
        //{
        //    Assert.That(_ddoc.TicksToAngularVelocity(10, deltaT: 2), Is.EqualTo(5 * MathHelper.TwoPi / 5).Within(1e-6));
        //}

        //[Test]
        //public void Velocity()
        //{
        //    float omegaR = 0;
        //    float omegaL = 0;
        //    float theta = 0;
        //    Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3()));

        //    omegaR = 1;
        //    omegaL = 1;
        //    theta = 0;
        //    Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(1, 0, 0)));

        //    omegaR = -1;
        //    omegaL = -1;
        //    theta = 0;
        //    Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(-1, 0, 0)));

        //    omegaR = 1;
        //    omegaL = -1;
        //    theta = 0;
        //    Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(0, 0, 2)));

        //    omegaR = 1;
        //    omegaL = 1;
        //    theta = MathHelper.PiOver4;
        //    Assert.That(_odometryCalc.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(1 / (float)Math.Sqrt(2), 1 / (float)Math.Sqrt(2), 0)));
        //}
    }
}
