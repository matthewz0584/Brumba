using Brumba.WaiterStupid;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;

namespace Brumba.DwaNavigator
{
    [DataContract]
    public struct VelocityAcceleration
    {
        public VelocityAcceleration(Velocity velocity, Vector2 wheelAcceleration)
            : this()
        {
            Velocity = velocity;
            WheelAcceleration = wheelAcceleration;
        }

        [DataMember]
        public Velocity Velocity { get; set; }
        [DataMember]
        public Vector2 WheelAcceleration { get; set; }
    }
}