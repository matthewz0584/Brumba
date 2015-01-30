using System;
using Brumba.Common;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DiffDriveOdometry
{
	public class DiffDriveOdometryCalculator
	{
        public DiffDriveOdometryCalculator(double wheelRadius, double wheelBase, int ticksPerRotation)
        {
            DC.Contract.Requires(wheelRadius > 0);
            DC.Contract.Requires(wheelBase > 0);
            DC.Contract.Requires(ticksPerRotation > 0);
            DC.Contract.Ensures(RadiansPerTick > 0);

            WheelRadius = wheelRadius;
            WheelBase = wheelBase;
            RadiansPerTick = MathHelper.TwoPi / ticksPerRotation;
        }

        public double WheelRadius { get; private set; }
        public double WheelBase { get; private set; }
        public double RadiansPerTick { get; private set; }

        public Pose CalculatePoseDelta(int leftTicksDelta, int rightTicksDelta, double oldTheta)
	    {
			DC.Contract.Requires(!double.IsNaN(oldTheta));
			DC.Contract.Requires(!double.IsInfinity(oldTheta));

            var dist = WheelsToRobotKinematics(new Vector2(leftTicksDelta, rightTicksDelta) * (float)RadiansPerTick);
            return new Pose((float)dist.Linear * new Vector2((float)Math.Cos(oldTheta), (float)Math.Sin(oldTheta)), dist.Angular);
	    }

        public Velocity CalculateVelocity(int leftTicksDelta, int rightTicksDelta, double deltaT)
        {
            DC.Contract.Requires(deltaT > 0);

            return WheelsToRobotKinematics(new Vector2(leftTicksDelta, rightTicksDelta) * (float)(RadiansPerTick / deltaT));
        }

        public Tuple<Pose, Velocity> UpdateOdometry(Pose previousPose, int leftTicksDelta, int rightTicksDelta, double deltaT)
        {
            DC.Contract.Requires(!double.IsNaN(previousPose.Bearing));
            DC.Contract.Requires(!double.IsInfinity(previousPose.Bearing));
            DC.Contract.Requires(deltaT > 0);
            DC.Contract.Ensures(DC.Contract.Result<Tuple<Pose, Velocity>>() != null);

            var poseDelta = CalculatePoseDelta(leftTicksDelta, rightTicksDelta, previousPose.Bearing);
            var velocity = CalculateVelocity(leftTicksDelta, rightTicksDelta, deltaT);
            return Tuple.Create(new Pose(previousPose.Position + poseDelta.Position, previousPose.Bearing + poseDelta.Bearing), velocity);
        }

	    public Velocity WheelsToRobotKinematics(Vector2 wheelsValues)
        {
            return new Velocity(WheelRadius / 2 * (wheelsValues.Y + wheelsValues.X), WheelRadius / WheelBase * (wheelsValues.Y - wheelsValues.X));
        }

        public Vector2 RobotKinematicsToWheels(Velocity v)
        {
            return new Vector2((float)(v.Linear * 2 - v.Angular * WheelBase), (float)(v.Linear * 2 + v.Angular * WheelBase)) / 2 / (float)WheelRadius;
        }
	}
}