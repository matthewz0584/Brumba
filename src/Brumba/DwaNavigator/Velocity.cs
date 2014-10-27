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

        public override string ToString()
        {
            return string.Format("(L:{0}, A:{1})", Linear, Angular);
        }
    }
}