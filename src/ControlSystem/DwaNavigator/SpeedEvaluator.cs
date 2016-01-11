using Brumba.Common;
using Brumba.Utils;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class SpeedEvaluator : IVelocityEvaluator
    {
        public double RobotMaxSpeed { get; private set; }

        public SpeedEvaluator(double robotMaxSpeed)
        {
            DC.Contract.Requires(robotMaxSpeed > 0);

            RobotMaxSpeed = robotMaxSpeed;
        }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Assert(v.Linear.BetweenRL(0, RobotMaxSpeed));

            return v.Linear / RobotMaxSpeed;
        }
    }
}