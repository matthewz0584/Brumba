using System;
using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DiffDriveOdometry
{
	public class DiffDriveOdometryCalculator
	{
        [DC.ContractInvariantMethod]
        void ObjectInvariant()
        {
            DC.Contract.Invariant(Constants.WheelBase > 0);
            DC.Contract.Invariant(Constants.WheelRadius > 0);
            DC.Contract.Invariant(Constants.TicksPerRotation > 0);
            DC.Contract.Invariant(Constants.RadiansPerTick > 0);
        }

		public DiffDriveOdometryCalculator(DiffDriveOdometryConstants constants)
		{
			Constants = constants;
		}

		public DiffDriveOdometryConstants Constants { get; set; }

	    public Pose CalculatePoseDelta(double oldTheta, int leftTicks, int rightTicks)
	    {
			DC.Contract.Requires(!double.IsNaN(oldTheta));
			DC.Contract.Requires(!double.IsInfinity(oldTheta));

            return new Pose(new Vector2((rightTicks + leftTicks) / 2f * (float)Math.Cos(oldTheta), (rightTicks + leftTicks) / 2f * (float)Math.Sin(oldTheta))
									* Constants.WheelRadius * Constants.RadiansPerTick,
							   (rightTicks - leftTicks) / Constants.WheelBase * Constants.WheelRadius * Constants.RadiansPerTick);
	    }

        public DiffDriveOdometryState UpdateOdometry(DiffDriveOdometryState previousDiffDriveOdometry, int leftTicks, int rightTicks)
        {
            DC.Contract.Requires(previousDiffDriveOdometry != null);
            DC.Contract.Requires(!double.IsNaN(previousDiffDriveOdometry.Pose.Bearing));
            DC.Contract.Requires(!double.IsInfinity(previousDiffDriveOdometry.Pose.Bearing));
            DC.Contract.Ensures(DC.Contract.Result<DiffDriveOdometryState>() != null);
            DC.Contract.Ensures(DC.Contract.Result<DiffDriveOdometryState>().LeftTicks == leftTicks);
            DC.Contract.Ensures(DC.Contract.Result<DiffDriveOdometryState>().RightTicks == rightTicks);

	        var poseDelta = CalculatePoseDelta(previousDiffDriveOdometry.Pose.Bearing,
		        leftTicks - previousDiffDriveOdometry.LeftTicks,
		        rightTicks - previousDiffDriveOdometry.RightTicks);
	        return new DiffDriveOdometryState
            {
                LeftTicks = leftTicks,
                RightTicks = rightTicks,
                Pose = new Pose(previousDiffDriveOdometry.Pose.Position + poseDelta.Position, previousDiffDriveOdometry.Pose.Bearing + poseDelta.Bearing)
            };
        }

        
        //public float TicksToAngularVelocity(int deltaTicks, float deltaT)
        //{
        //    DC.Contract.Requires(deltaT > 0);
        //    DC.Contract.Requires(Constants.TicksPerRotation > 0);
        //    DC.Contract.Requires(Constants.RadiansPerTick > 0);

        //    return deltaTicks / deltaT * Constants.RadiansPerTick;
        //}

        //public Vector3 CalculateVelocity(float omegaR, float omegaL, float theta)
        //{
        //    DC.Contract.Requires(Constants.WheelRadius > 0);
        //    DC.Contract.Requires(Constants.WheelBase > 0);

        //    return new Vector3(Constants.WheelRadius / 2 * (omegaR + omegaL) * (float)Math.Cos(theta),
        //                       Constants.WheelRadius / 2 * (omegaR + omegaL) * (float)Math.Sin(theta),
        //                       Constants.WheelRadius / Constants.WheelBase * (omegaR - omegaL));
        //}
	}
}