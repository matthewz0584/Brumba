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

        public Pose CalculatePoseDelta(int leftTicksDelta, int rightTicksDelta)
	    {
            var dist = WheelsToRobotKinematics(new Vector2(leftTicksDelta, rightTicksDelta) * (float)RadiansPerTick);

            return (leftTicksDelta == rightTicksDelta ? (IMotionModel)new LinearMotionModel(dist.Linear) : new CirclularMotionModel(dist)).PredictPoseDeltaAsForDistance();
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

            return Tuple.Create(MergeSequentialPoseDeltas(previousPose, CalculatePoseDelta(leftTicksDelta, rightTicksDelta)),
                CalculateVelocity(leftTicksDelta, rightTicksDelta, deltaT));
        }

	    public Velocity WheelsToRobotKinematics(Vector2 wheelsValues)
        {
            return new Velocity(WheelRadius / 2 * (wheelsValues.Y + wheelsValues.X), WheelRadius / WheelBase * (wheelsValues.Y - wheelsValues.X));
        }

        public Vector2 RobotKinematicsToWheels(Velocity v)
        {
            return new Vector2((float)(v.Linear * 2 - v.Angular * WheelBase), (float)(v.Linear * 2 + v.Angular * WheelBase)) / 2 / (float)WheelRadius;
        }

        public static Pose MergeSequentialPoseDeltas(Pose delta1, Pose delta2)
        {
            var originTransform = Matrix.CreateRotationZ((float)delta1.Bearing) * Matrix.CreateTranslation(new Vector3(delta1.Position, 0));
            return new Pose(Vector2.Transform(delta2.Position, originTransform), delta1.Bearing + delta2.Bearing);
        }
	}
}