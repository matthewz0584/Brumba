using System;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.WaiterStupid.Odometry
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

	    public Vector3 CalculatePoseDelta(float oldTheta, int leftTicks, int rightTicks)
	    {
            DC.Contract.Requires(!float.IsNaN(oldTheta));
            DC.Contract.Requires(!float.IsInfinity(oldTheta));

            return new Vector3((rightTicks + leftTicks) / 2f * (float)Math.Cos(oldTheta),
                               (rightTicks + leftTicks) / 2f * (float)Math.Sin(oldTheta),
                               (rightTicks - leftTicks) / Constants.WheelBase) * 
                   Constants.WheelRadius * Constants.RadiansPerTick;
	    }

        public DiffDriveOdometryState UpdateOdometry(DiffDriveOdometryState previousDiffDriveOdometry, int leftTicks, int rightTicks)
        {
            DC.Contract.Requires(previousDiffDriveOdometry != null);
            DC.Contract.Requires(!float.IsNaN(previousDiffDriveOdometry.Pose.Z));
            DC.Contract.Requires(!float.IsInfinity(previousDiffDriveOdometry.Pose.Z));
            DC.Contract.Ensures(DC.Contract.Result<DiffDriveOdometryState>() != null);
            DC.Contract.Ensures(DC.Contract.Result<DiffDriveOdometryState>().LeftTicks == leftTicks);
            DC.Contract.Ensures(DC.Contract.Result<DiffDriveOdometryState>().RightTicks == rightTicks);

            return new DiffDriveOdometryState
            {
                LeftTicks = leftTicks,
                RightTicks = rightTicks,
                Pose = previousDiffDriveOdometry.Pose + CalculatePoseDelta(previousDiffDriveOdometry.Pose.Z,
                                                                    leftTicks - previousDiffDriveOdometry.LeftTicks,
                                                                    rightTicks - previousDiffDriveOdometry.RightTicks),
            };
        }
	}
}