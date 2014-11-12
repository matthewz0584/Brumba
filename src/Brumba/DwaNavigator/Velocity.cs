using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;

namespace Brumba.DwaNavigator
{
    [DataContract]
    public struct Velocity
    {
        public Velocity(double linear, double angular) : this()
        {
            Linear = linear;
            Angular = angular;
        }

        [DataMember]
        public double Linear { get; set; }
        [DataMember]
        public double Angular { get; set; }

        public override string ToString()
        {
            return string.Format("(L:{0}, A:{1})", Linear, Angular);
        }
    }

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