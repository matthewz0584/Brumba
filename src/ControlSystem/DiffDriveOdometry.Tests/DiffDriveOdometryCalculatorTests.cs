using Brumba.Common;
using Brumba.Utils;
using MathNet.Numerics;
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
            var pd = _ddoc.CalculatePoseDelta(leftTicksDelta: 10, rightTicksDelta: 10);
            Assert.That(pd.Position.EqualsRelatively(new Vector2(2 * MathHelper.TwoPi, 0), 1e-5));
            Assert.That(pd.Bearing, Is.EqualTo(0));

            pd = _ddoc.CalculatePoseDelta(leftTicksDelta: -10, rightTicksDelta: -10);
            Assert.That(pd.Position.EqualsRelatively(new Vector2(-2 * MathHelper.TwoPi, 0), 1e-5));
            Assert.That(pd.Bearing, Is.EqualTo(0));

            pd = _ddoc.CalculatePoseDelta(leftTicksDelta: -10, rightTicksDelta: 10);
            Assert.That(pd.Position.EqualsWithError(new Vector2(), 1e-5));
            Assert.That(pd.Bearing, Is.EqualTo(2 * MathHelper.TwoPi / (4 / 2)));

            //One quarter of circle of radius 6: outer wheel traverses twice as much as inner =>
            //its radius is also twice as much => equals 8 => outer distance equals to 4pi which equals 10 ticks
            pd = _ddoc.CalculatePoseDelta(leftTicksDelta: 5, rightTicksDelta: 10);
            Assert.That(pd.Position.EqualsRelatively(new Vector2(6, 6), 1e-5));
            Assert.That(pd.Bearing, Is.EqualTo(MathHelper.PiOver2));
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

        [Test]
        public void MergeSequentialPoseDeltas()
        {
            Assert.That(DiffDriveOdometryCalculator.MergeSequentialPoseDeltas(new Pose(new Vector2(1, 2), 0), new Pose(new Vector2(2, -3), 0)),
                Is.EqualTo(new Pose(new Vector2(3, -1), 0)));

            Assert.That(DiffDriveOdometryCalculator.MergeSequentialPoseDeltas(new Pose(new Vector2(), Constants.PiOver4), new Pose(new Vector2(), -Constants.Pi / 8)),
                Is.EqualTo(new Pose(new Vector2(), Constants.Pi / 8)));

            Assert.That(DiffDriveOdometryCalculator.MergeSequentialPoseDeltas(new Pose(new Vector2(1, 2), Constants.PiOver2), new Pose(new Vector2(2, 0), -Constants.PiOver4)).Position.
                EqualsWithError(new Vector2(1, 4), 1e-7));
            Assert.That(DiffDriveOdometryCalculator.MergeSequentialPoseDeltas(new Pose(new Vector2(1, 2), Constants.PiOver2), new Pose(new Vector2(2, 0), -Constants.PiOver4)).Bearing,
                Is.EqualTo(Constants.PiOver4));
        }
    }
}
