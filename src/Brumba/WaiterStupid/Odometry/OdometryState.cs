using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.WaiterStupid.Odometry
{
    [DataContract]
    public class OdometryState
    {
        [DataMember]
        public Vector3 Pose { get; set; }
        [DataMember]
        public Vector3 Velocity { get; set; }
        [DataMember]
        public Vector3 PoseDelta { get; set; }
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
            get
            {
                DC.Contract.Requires(TicksPerRotation > 0);
                DC.Contract.Ensures(DC.Contract.Result<float>() > 0);

                return MathHelper.TwoPi / TicksPerRotation;
            }
        }
    }
}