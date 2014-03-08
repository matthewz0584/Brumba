using System;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.WaiterStupid.Odometry
{
	public class OdometryCalculator
	{
        [DC.ContractInvariantMethod]
        void ObjectInvariant()
        {
            DC.Contract.Invariant(Constants != null);
        }

		public OdometryCalculator()
		{
            DC.Contract.Ensures(Constants != null);

			Constants = new OdometryConstants();
		}

		public OdometryConstants Constants { get; set; }

		public float TicksToAngularVelocity(int deltaTicks, float deltaT)
		{
            DC.Contract.Requires(deltaT > 0);
            DC.Contract.Requires(Constants.TicksPerRotation > 0);
            DC.Contract.Requires(Constants.RadiansPerTick > 0);

			return deltaTicks / deltaT * Constants.RadiansPerTick;
		}

		public Vector3 CalculateVelocity(float omegaR, float omegaL, float theta)
		{
            DC.Contract.Requires(Constants.WheelRadius > 0);
            DC.Contract.Requires(Constants.WheelBase > 0);

			return new Vector3(Constants.WheelRadius / 2 * (omegaR + omegaL) * (float)Math.Cos(theta),
							   Constants.WheelRadius / 2 * (omegaR + omegaL) * (float)Math.Sin(theta),
							   Constants.WheelRadius / Constants.WheelBase * (omegaR - omegaL));
		}

		public Vector3 UpdatePose(Vector3 oldPose, Vector3 velocity, float deltaT)
		{
            DC.Contract.Requires(deltaT >= 0);

			return oldPose + velocity * deltaT;
		}

		public OdometryState UpdateOdometry(OdometryState previousOdometry, float deltaT, int leftTicks, int rightTicks)
		{
            DC.Contract.Requires(previousOdometry != null);
            DC.Contract.Requires(deltaT > 0);
            DC.Contract.Requires(Constants.TicksPerRotation > 0);
            DC.Contract.Ensures(DC.Contract.Result<OdometryState>() != null);
            DC.Contract.Ensures(DC.Contract.Result<OdometryState>().LeftTicks == leftTicks);
            DC.Contract.Ensures(DC.Contract.Result<OdometryState>().RightTicks == rightTicks);

			var newOdometry = new OdometryState {LeftTicks = leftTicks, RightTicks = rightTicks};
			newOdometry.Velocity = CalculateVelocity(
				TicksToAngularVelocity(newOdometry.RightTicks - previousOdometry.RightTicks, deltaT),
				TicksToAngularVelocity(newOdometry.LeftTicks - previousOdometry.LeftTicks, deltaT),
				previousOdometry.Pose.Z);
			newOdometry.Pose = UpdatePose(previousOdometry.Pose, 0.5f*(newOdometry.Velocity + previousOdometry.Velocity), deltaT);
		    newOdometry.PoseDelta = newOdometry.Pose - previousOdometry.Pose;
			return newOdometry;
		}

	    public Vector3 CalculatePoseDelta(float oldTheta, int leftTicks, int rightTicks)
	    {
            return new Vector3((rightTicks + leftTicks) / 2f * (float)Math.Cos(oldTheta),
                               (rightTicks + leftTicks) / 2f * (float)Math.Sin(oldTheta),
                               (rightTicks - leftTicks) / Constants.WheelBase) * 
                   Constants.WheelRadius * Constants.RadiansPerTick;
	    }
	}
}