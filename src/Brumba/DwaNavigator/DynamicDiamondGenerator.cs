using System.Collections.Generic;
using System.Linq;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class DynamicDiamondGenerator
    {
        public const int STEPS_NUMBER = 10;

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

        public Velocity AccelerationMax
        {
            get
            {
                DC.Contract.Ensures(DC.Contract.Result<Velocity>().Linear > 0 && DC.Contract.Result<Velocity>().Angular > 0);

                return new Velocity(
                    WheelsToAcceleration(WheelAngularAccelerationMax, WheelAngularAccelerationMax).Linear,
                    WheelsToAcceleration(-WheelAngularAccelerationMax, WheelAngularAccelerationMax).Angular);
            }
        }

        public Velocity VelocityStep
        {
            get
            {
                DC.Contract.Ensures(DC.Contract.Result<Velocity>().Linear > 0 && DC.Contract.Result<Velocity>().Angular > 0);

                return new Velocity(AccelerationMax.Linear * Dt / STEPS_NUMBER, AccelerationMax.Angular * Dt / STEPS_NUMBER);
            }
        }

        public IEnumerable<Velocity> Generate(Velocity diamondCenter)
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Velocity>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Velocity>>().Count() == (2 * STEPS_NUMBER + 1) * (2 * STEPS_NUMBER + 1));

            return Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).
                SelectMany(wli => 
                    Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).Select(wri =>
                    {
                        var a = WheelsToAcceleration(wli * WheelAccelerationStep, wri * WheelAccelerationStep);
                        return new Velocity(diamondCenter.Linear + a.Linear * Dt, diamondCenter.Angular + a.Angular * Dt);
                    }));
                    //.Where(v => v.Linear >= 0);
        }

        Velocity WheelsToAcceleration(double wl, double wr)
        {
            return new Velocity(WheelRadius / 2 * (wl + wr), WheelRadius / WheelBase * (wr - wl));
        }

        double WheelAccelerationStep
        {
            get
            {
                DC.Contract.Ensures(DC.Contract.Result<double>() > 0);
                return WheelAngularAccelerationMax / STEPS_NUMBER;
            }
        }
    }
}