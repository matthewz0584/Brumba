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
            DC.Contract.Invariant(Constants.WheelBase > 0);
            DC.Contract.Invariant(Constants.WheelRadius > 0);
            DC.Contract.Invariant(Constants.TicksPerRotation > 0);
            DC.Contract.Invariant(Constants.RadiansPerTick > 0);
        }

		public OdometryCalculator(OdometryConstants constants)
		{
			Constants = constants;
		}

		public OdometryConstants Constants { get; set; }

	    public Vector3 CalculatePoseDelta(float oldTheta, int leftTicks, int rightTicks)
	    {
            DC.Contract.Requires(!float.IsNaN(oldTheta));
            DC.Contract.Requires(!float.IsInfinity(oldTheta));

            return new Vector3((rightTicks + leftTicks) / 2f * (float)Math.Cos(oldTheta),
                               (rightTicks + leftTicks) / 2f * (float)Math.Sin(oldTheta),
                               (rightTicks - leftTicks) / Constants.WheelBase) * 
                   Constants.WheelRadius * Constants.RadiansPerTick;
	    }

        public OdometryState UpdateOdometry(OdometryState previousOdometry, int leftTicks, int rightTicks)
        {
            DC.Contract.Requires(previousOdometry != null);
            DC.Contract.Requires(!float.IsNaN(previousOdometry.Pose.Z));
            DC.Contract.Requires(!float.IsInfinity(previousOdometry.Pose.Z));
            DC.Contract.Ensures(DC.Contract.Result<OdometryState>() != null);
            DC.Contract.Ensures(DC.Contract.Result<OdometryState>().LeftTicks == leftTicks);
            DC.Contract.Ensures(DC.Contract.Result<OdometryState>().RightTicks == rightTicks);

            return new OdometryState
            {
                LeftTicks = leftTicks,
                RightTicks = rightTicks,
                Pose = previousOdometry.Pose + CalculatePoseDelta(previousOdometry.Pose.Z,
                                                                    leftTicks - previousOdometry.LeftTicks,
                                                                    rightTicks - previousOdometry.RightTicks),
            };
        }
	}
}