using Brumba.Utils;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class SpeedEvaluator : IVelocityEvaluator
    {
        public double MaxSpeed { get; private set; }

        public SpeedEvaluator(double maxSpeed)
        {
            DC.Contract.Requires(maxSpeed > 0);

            MaxSpeed = maxSpeed;
        }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Assert(v.Linear.BetweenRL(0, MaxSpeed));

            return v.Linear / MaxSpeed;
        }
    }
}