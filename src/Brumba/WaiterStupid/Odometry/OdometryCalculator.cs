using System;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.Odometry
{
	public class OdometryCalculator
	{
		public OdometryCalculator()
		{
			Constants = new OdometryConstants();
		}

		public OdometryConstants Constants { get; set; }

		public float TicksToAngularVelocity(int deltaTicks, float deltaT)
		{
			return deltaTicks / deltaT * Constants.RadiansPerTick;
		}

		public Vector3 CalculateVelocity(float omegaR, float omegaL, float theta)
		{
			return new Vector3(Constants.WheelRadius / 2 * (omegaR + omegaL) * (float)Math.Cos(theta),
							   Constants.WheelRadius / 2 * (omegaR + omegaL) * (float)Math.Sin(theta),
							   Constants.WheelRadius / Constants.WheelBase * (omegaR - omegaL));
		}

		public Vector3 CalculatePose(Vector3 oldPose, Vector3 velocity, float deltaT)
		{
			return oldPose + velocity * deltaT;
		}

		public OdometryState UpdateOdometry(OdometryState previousOdometry, float deltaT, int leftTicks, int rightTicks)
		{
			var newOdometry = new OdometryState {LeftTicks = leftTicks, RightTicks = rightTicks};
			newOdometry.Velocity = CalculateVelocity(
				TicksToAngularVelocity(newOdometry.RightTicks - previousOdometry.RightTicks, deltaT),
				TicksToAngularVelocity(newOdometry.LeftTicks - previousOdometry.LeftTicks, deltaT),
				previousOdometry.Pose.Z);
			newOdometry.Pose = CalculatePose(previousOdometry.Pose, 0.5f*(newOdometry.Velocity + previousOdometry.Velocity), deltaT);
			return newOdometry;
		}
	}

	[DataContract]
	public class OdometryState
	{
		[DataMember]
		public Vector3 Pose { get; set; }
		[DataMember]
		public Vector3 Velocity { get; set; }
		[DataMember]
		public int LeftTicks { get; set; }
		[DataMember]
		public int RightTicks { get; set; }
	}

	[DataContract]
	public class OdometryConstants
	{
		[DataMember, DataMemberConstructor]
		public int TicksPerRotation { get; set; }

		[DataMember, DataMemberConstructor]
		public float WheelRadius { get; set; }

		[DataMember, DataMemberConstructor]
		public float WheelBase { get; set; }

		[DataMember, DataMemberConstructor]
		public float DeltaT { get; set; }

		public float RadiansPerTick
		{
			get { return MathHelper.TwoPi / TicksPerRotation; }
		}
	}
}