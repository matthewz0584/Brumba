using System;
using Brumba.WaiterStupid.Odometry;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
	[TestFixture]
    public class OdometryTests
    {
		private OdometryCalculator m_odometry;

		[SetUp]
		public void SetUp()
		{
			m_odometry = new OdometryCalculator { Constants = { WheelBase = 1, WheelRadius = 1 } };
		}

		[Test]
		public void TicksToAngularVelocity()
		{
			float deltaT = 2;
			Assert.That(m_odometry.TicksToAngularVelocity(10, deltaT), Is.EqualTo(5 * m_odometry.Constants.RadiansPerTick).Within(1e-6));
		}

		[Test]
		public void Velocity()
		{
			float omegaR = 0;
			float omegaL = 0;
			float theta = 0;
			Assert.That(m_odometry.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3()));

			omegaR = 1;
			omegaL = 1;
			theta = 0;
			Assert.That(m_odometry.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(1, 0, 0)));

			omegaR = -1;
			omegaL = -1;
			theta = 0;
			Assert.That(m_odometry.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(-1, 0, 0)));

			omegaR = 1;
			omegaL = -1;
			theta = 0;
			Assert.That(m_odometry.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(0, 0, 2)));

			omegaR = 1;
			omegaL = 1;
			theta = MathHelper.PiOver4;
			Assert.That(m_odometry.CalculateVelocity(omegaR, omegaL, theta), Is.EqualTo(new Vector3(1 / (float)Math.Sqrt(2), 1 / (float)Math.Sqrt(2), 0)));
		}

		[Test]
		public void Pose()
		{
			var oldPose = new Vector3();
			var velocity = new Vector3();
			var deltaT = 0.1f;
			Assert.That(m_odometry.CalculatePose(oldPose, velocity, deltaT), Is.EqualTo(new Vector3()));

			oldPose = new Vector3();
			velocity = new Vector3(1, 1, 0);
			Assert.That(m_odometry.CalculatePose(oldPose, velocity, deltaT), Is.EqualTo(new Vector3(0.1f, 0.1f, 0)));

			oldPose = new Vector3();
			velocity = new Vector3(0, 0, 1);
			Assert.That(m_odometry.CalculatePose(oldPose, velocity, deltaT), Is.EqualTo(new Vector3(0, 0, 0.1f)));

			velocity = new Vector3(0, 0, 1);
			Assert.That(m_odometry.CalculatePose(oldPose, velocity, 10), Is.EqualTo(new Vector3(0, 0, 10f % MathHelper.TwoPi)));
		}

		[Test]
		public void Acceptance()
		{
			m_odometry.Constants.TicksPerRotation = 2;
			m_odometry.Constants.WheelRadius = 1 / MathHelper.Pi;
			var previousOdometry = new OdometryState {LeftTicks = 2, RightTicks = 2};
			var deltaT = 0.1f;
			var leftTicks = 2;
			var rightTicks = 2;

			var newOdometry = m_odometry.UpdateOdometry(previousOdometry, deltaT, leftTicks, rightTicks);

			Assert.That(newOdometry.LeftTicks, Is.EqualTo(2));
			Assert.That(newOdometry.RightTicks, Is.EqualTo(2));
			Assert.That(newOdometry.Velocity, Is.EqualTo(new Vector3()));
			Assert.That(newOdometry.Pose, Is.EqualTo(new Vector3()));

			previousOdometry = new OdometryState { LeftTicks = 0, RightTicks = 0 };
			newOdometry = m_odometry.UpdateOdometry(previousOdometry, deltaT, leftTicks, rightTicks);

			Assert.That(newOdometry.LeftTicks, Is.EqualTo(2));
			Assert.That(newOdometry.RightTicks, Is.EqualTo(2));
			Assert.That(newOdometry.Velocity, Is.EqualTo(new Vector3(20, 0, 0)));
			Assert.That(newOdometry.Pose, Is.EqualTo(0.5f * new Vector3(20, 0, 0) * deltaT));
		}
    }
}
