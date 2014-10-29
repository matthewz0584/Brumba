using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    [DC.ContractClassAttribute(typeof(IVelocitySearchSpaceGeneratorContract))]
    public interface IVelocitySearchSpaceGenerator
    {
        IDictionary<Velocity, Vector2> Generate(Velocity center);
    }

    [DC.ContractClassForAttribute(typeof(IVelocitySearchSpaceGenerator))]
    abstract class IVelocitySearchSpaceGeneratorContract : IVelocitySearchSpaceGenerator
    {
        public IDictionary<Velocity, Vector2> Generate(Velocity center)
        {
            DC.Contract.Ensures(DC.Contract.Result<IDictionary<Velocity, Vector2>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IDictionary<Velocity, Vector2>>().ContainsKey(center));

            return default(IDictionary<Velocity, Vector2>);
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

        public IDictionary<Velocity, Vector2> Generate(Velocity diamondCenter)
        {
            DC.Contract.Ensures(DC.Contract.Result<IDictionary<Velocity, Vector2>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IDictionary<Velocity, Vector2>>().Count() == (2 * STEPS_NUMBER + 1) * (2 * STEPS_NUMBER + 1));
            DC.Contract.Ensures(DC.Contract.Result<IDictionary<Velocity, Vector2>>().Values.All(war => Math.Abs(war.X) <= 1 && Math.Abs(war.Y) <= 1));

            return GenerateWheelAccelerationGrid().
                    Select(p => new
                    {
                        Velocity = new Velocity(
                            diamondCenter.Linear + WheelAccelerationToAcceleration(p).Linear * Dt,
                            diamondCenter.Angular + WheelAccelerationToAcceleration(p).Angular * Dt),
                        WheelAccelerationRelative = p / (float)WheelAngularAccelerationMax
                    }).
                ToDictionary(vwar => vwar.Velocity, vwar => vwar.WheelAccelerationRelative);
        }

        IEnumerable<Vector2> GenerateWheelAccelerationGrid()
        {
            return Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).
                SelectMany(wli =>
                    Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).
                    Select(wri => new Vector2(wli, wri) * (float)(WheelAngularAccelerationMax / STEPS_NUMBER)));
        }
            
        Velocity WheelAccelerationToAcceleration(Vector2 wheelAcc)
        {
            return new Velocity(WheelRadius / 2 * (wheelAcc.Y + wheelAcc.X), WheelRadius / WheelBase * (wheelAcc.Y - wheelAcc.X));
        }
    }
}