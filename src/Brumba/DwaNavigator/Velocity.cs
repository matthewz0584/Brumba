namespace Brumba.DwaNavigator
{
    public struct Velocity
    {
        public Velocity(double linear, double angular) : this()
        {
            Linear = linear;
            Angular = angular;
        }

        public double Linear { get; set; }
        public double Angular { get; set; }
    }
}