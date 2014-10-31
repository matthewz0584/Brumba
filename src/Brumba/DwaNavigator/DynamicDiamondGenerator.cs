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
        public const int STEPS_NUMBER = 10;

        public DynamicDiamondGenerator(double wheelAngularAccelerationMax, double wheelRadius, double wheelBase, double dt)
        {
            DC.Contract.Requires(wheelAngularAccelerationMax > 0);
            DC.Contract.Requires(wheelRadius > 0);
            DC.Contract.Requires(wheelBase > 0);
            DC.Contract.Requires(dt > 0);
            DC.Contract.Ensures(AccelerationMax.Linear > 0 && AccelerationMax.Angular > 0);
            DC.Contract.Ensures(VelocityStep.Linear > 0 && VelocityStep.Angular > 0);

            WheelAngularAccelerationMax = wheelAngularAccelerationMax;
            WheelRadius = wheelRadius;
            WheelBase = wheelBase;
            Dt = dt;

            AccelerationMax = new Velocity(
                    WheelAccelerationToAcceleration(new Vector2((float)WheelAngularAccelerationMax, (float)WheelAngularAccelerationMax)).Linear,
                    WheelAccelerationToAcceleration(new Vector2(-(float)WheelAngularAccelerationMax, (float)WheelAngularAccelerationMax)).Angular);

            VelocityStep = new Velocity(AccelerationMax.Linear * Dt / STEPS_NUMBER, AccelerationMax.Angular * Dt / STEPS_NUMBER);
        }

        public double WheelAngularAccelerationMax { get; private set; }
        public double WheelRadius { get; private set; }
        public double WheelBase { get; private set; }
        public double Dt { get; set; }

        public Velocity AccelerationMax { get; private set; }
        public Velocity VelocityStep { get; private set; }

        public VelocityAcceleration[,] Generate(Velocity diamondCenter)
        {
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>() != null);
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>().GetLength(0).BetweenRL(STEPS_NUMBER + 1, 2 * STEPS_NUMBER + 1));
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>().GetLength(1) == 2 * STEPS_NUMBER + 1);

            var velocitySpace = new VelocityAcceleration[2 * STEPS_NUMBER + 1, 2 * STEPS_NUMBER + 1];
            foreach (var p in GenerateWheelAccelerationGrid())
            {
                var wheelAcc = p.ToVec() * (float)(WheelAngularAccelerationMax / STEPS_NUMBER);
                var acc = WheelAccelerationToAcceleration(wheelAcc);
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

        Velocity WheelAccelerationToAcceleration(Vector2 wheelAcc)
        {
            return new Velocity(WheelRadius / 2 * (wheelAcc.Y + wheelAcc.X), WheelRadius / WheelBase * (wheelAcc.Y - wheelAcc.X));
        }
    }

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