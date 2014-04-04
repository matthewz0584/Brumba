using Brumba.WaiterStupid;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DiffDriveOdometry
{
    [DataContract]
    public class DiffDriveOdometryState
    {
        [DataMember]
        public Pose Pose { get; set; }
        [DataMember]
        public int LeftTicks { get; set; }
        [DataMember]
        public int RightTicks { get; set; }
    }

    [DataContract]
    public struct DiffDriveOdometryConstants
    {
        [DataMember, DataMemberConstructor]
        public int TicksPerRotation { get; set; }

        [DataMember, DataMemberConstructor]
        public float WheelRadius { get; set; }

        [DataMember, DataMemberConstructor]
        public float WheelBase { get; set; }

        public float RadiansPerTick
        {
            get
            {
                DC.Contract.Requires(TicksPerRotation > 0);
                DC.Contract.Ensures(DC.Contract.Result<float>() > 0);

                return MathHelper.TwoPi / TicksPerRotation;
            }
        }
    }
}