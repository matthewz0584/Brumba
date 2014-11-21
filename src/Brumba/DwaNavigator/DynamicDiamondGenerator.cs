using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    [DC.ContractClassAttribute(typeof(IVelocitySearchSpaceGeneratorContract))]
    public interface IVelocitySearchSpaceGenerator
    {
        VelocityAcceleration[,] Generate(Velocity center);
    }

    [DC.ContractClassForAttribute(typeof(IVelocitySearchSpaceGenerator))]
    abstract class IVelocitySearchSpaceGeneratorContract : IVelocitySearchSpaceGenerator
    {
        public VelocityAcceleration[,] Generate(Velocity center)
        {
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>() != null);

            return default(VelocityAcceleration[,]);
        }
    }

    public class DynamicDiamondGenerator : IVelocitySearchSpaceGenerator
    {
        public const int STEPS_NUMBER = 5;

        public DynamicDiamondGenerator(double wheelAngularAccelerationMax, double wheelRadius, double wheelBase, double dt)
        {
            DC.Contract.Requires(wheelAngularAccelerationMax > 0);
            DC.Contract.Requires(wheelRadius > 0);
            DC.Contract.Requires(wheelBase > 0);
            DC.Contract.Requires(dt > 0);

            WheelAngularAccelerationMax = wheelAngularAccelerationMax;
            WheelRadius = wheelRadius;
            WheelBase = wheelBase;
            Dt = dt;
        }

        public double WheelAngularAccelerationMax { get; private set; }
        public double WheelRadius { get; private set; }
        public double WheelBase { get; private set; }
        public double Dt { get; set; }

        public VelocityAcceleration[,] Generate(Velocity diamondCenter)
        {
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>() != null);
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>().GetLength(0) == 2 * STEPS_NUMBER + 1);
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>().GetLength(1) == 2 * STEPS_NUMBER + 1);

            var velocitySpace = new VelocityAcceleration[2 * STEPS_NUMBER + 1, 2 * STEPS_NUMBER + 1];
            foreach (var p in GenerateWheelAccelerationGrid())
            {
                var wheelAcc = p.ToVec() * (float)(WheelAngularAccelerationMax / STEPS_NUMBER);
                var acc = WheelsToRobotKinematics(wheelAcc);
                velocitySpace[p.X + STEPS_NUMBER, p.Y + STEPS_NUMBER] = new VelocityAcceleration(
                    new Velocity(diamondCenter.Linear + acc.Linear * Dt, diamondCenter.Angular + acc.Angular * Dt),
                    wheelAcc / (float)WheelAngularAccelerationMax);
            }
            return velocitySpace;
        }

        IEnumerable<Point> GenerateWheelAccelerationGrid()
        {
            return Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).
                SelectMany(wri => Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).Select(wli => new Point(wli, wri)));
        }

        public Velocity WheelsToRobotKinematics(Vector2 wheelsValues)
        {
            return new Velocity(WheelRadius / 2 * (wheelsValues.Y + wheelsValues.X), WheelRadius / WheelBase * (wheelsValues.Y - wheelsValues.X));
        }
    }
}