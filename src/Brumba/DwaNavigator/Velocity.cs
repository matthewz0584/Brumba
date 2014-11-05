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

        public double Linear { get; set; }
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

        public Velocity Velocity { get; private set; }
        public Vector2 WheelAcceleration { get; private set; }
    }
}