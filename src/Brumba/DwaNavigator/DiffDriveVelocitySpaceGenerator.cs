using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Common;
using Brumba.DiffDriveOdometry;
using Brumba.Utils;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    [DC.ContractClassAttribute(typeof(IVelocitySpaceGeneratorContract))]
    public interface IVelocitySpaceGenerator
    {
        VelocityAcceleration[,] Generate(Velocity center);
    }

    [DC.ContractClassForAttribute(typeof(IVelocitySpaceGenerator))]
    abstract class IVelocitySpaceGeneratorContract : IVelocitySpaceGenerator
    {
        public VelocityAcceleration[,] Generate(Velocity center)
        {
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>() != null);

            return default(VelocityAcceleration[,]);
        }
    }

    [DC.ContractClassAttribute(typeof(IVelocityPredictorContract))]
    public interface IVelocityPredictor
    {
        Velocity PredictVelocity(Velocity velocityCurrent, Vector2 currents, double dt);
    }

    [DC.ContractClassForAttribute(typeof(IVelocityPredictor))]
    abstract class IVelocityPredictorContract : IVelocityPredictor
    {
        public Velocity PredictVelocity(Velocity velocityCurrent, Vector2 currents, double dt)
        {
            return default(Velocity);
        }
    }

    public class DiffDriveVelocitySpaceGenerator : IVelocitySpaceGenerator, IVelocityPredictor
    {
        public const int STEPS_NUMBER = 10;

        //readonly double _a;
        //readonly double _b;
        readonly double _c;
        readonly double _d;
        readonly double _wheelVelocityToTorque;
        DiffDriveOdometryCalculator _diffDriveOdometryCalc;

        public DiffDriveVelocitySpaceGenerator(
                double robotMass, double robotInertiaMoment, double wheelRadius, double wheelBase,
                double velocityMax, double currentToTorque, double frictionTorque, double dt)
        {
            DC.Contract.Requires(robotMass > 0);
            DC.Contract.Requires(robotInertiaMoment > 0);
            DC.Contract.Requires(velocityMax > 0);
            DC.Contract.Requires(wheelRadius > 0);
            DC.Contract.Requires(wheelBase > 0);
            DC.Contract.Requires(currentToTorque >= 0);
            DC.Contract.Requires(frictionTorque >= 0);
            DC.Contract.Requires(dt > 0);

            CurrentToTorque = currentToTorque;
            Dt = dt;

            _wheelVelocityToTorque = (currentToTorque - frictionTorque) / (velocityMax / wheelRadius);

            var massCoef = 1 / (robotMass * wheelRadius * wheelRadius);
            var momentCoef = wheelBase / 2 * wheelBase / 2 / (robotInertiaMoment * wheelRadius * wheelRadius);
            //_a = currentToTorque * massCoef;
            //_b = currentToTorque * momentCoef;
            _c = _wheelVelocityToTorque * massCoef;
            _d = _wheelVelocityToTorque * momentCoef;

            _diffDriveOdometryCalc = new DiffDriveOdometryCalculator(wheelRadius, wheelBase, int.MaxValue);
        }

        public double CurrentToTorque { get; private set; }
        public double Dt { get; private set; }

        public VelocityAcceleration[,] Generate(Velocity diamondCenter)
        {
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>() != null);
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>().GetLength(0) == 2 * STEPS_NUMBER + 1);
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>().GetLength(1) == 2 * STEPS_NUMBER + 1);

            var velocitySpace = new VelocityAcceleration[2 * STEPS_NUMBER + 1, 2 * STEPS_NUMBER + 1];
            foreach (var p in GenerateWheelAccelerationGrid())
                //Calculate omega values for Dt/2, I hope that it will represent reality closer than value at the end of period
                velocitySpace[p.X + STEPS_NUMBER, p.Y + STEPS_NUMBER] = new VelocityAcceleration(
                        PredictVelocity(diamondCenter, p.ToVec() / STEPS_NUMBER, Dt / 2),
                        p.ToVec() / STEPS_NUMBER);
            return velocitySpace;
        }

        public Velocity PredictVelocity(Velocity velocityCurrent, Vector2 currents, double dt)
        {
            return _diffDriveOdometryCalc.WheelsToRobotKinematics(PredictWheelVelocities(
                            _diffDriveOdometryCalc.RobotKinematicsToWheels(velocityCurrent), currents, dt));
        }

        public Vector2 PredictWheelVelocities(Vector2 omegaCurrent, Vector2 currents, double dt)
        {
            DC.Contract.Requires(currents.BetweenRL(new Vector2(-1, -1), new Vector2(1, 1)));
            DC.Contract.Requires(dt >= 0);

            //Linear approx
            var iSum = currents.X + currents.Y;
            var iDiff = currents.X - currents.Y;
            var omegaSum = omegaCurrent.X + omegaCurrent.Y;
            var omegaDiff = omegaCurrent.X - omegaCurrent.Y;
            //var omegaDotL = _a * iSum + _b * iDiff - _c * omegaSum - _d * omegaDiff;
            //var omegaDotR = _a * iSum - _b * iDiff - _c * omegaSum + _d * omegaDiff;

            //return omegaCurrent + new Vector2((float)omegaDotL, (float)omegaDotR) * (float)Dt;

            //Correct diff eq
            var c1 = (omegaSum - CurrentToTorque / _wheelVelocityToTorque * iSum) / 2;
            var c2 = (omegaDiff - CurrentToTorque / _wheelVelocityToTorque * iDiff) / 2;

            var omegaL = c1 * Math.Exp(-2 * _c * dt) + c2 * Math.Exp(-2 * _d * dt) + CurrentToTorque / _wheelVelocityToTorque * currents.X;
            var omegaR = c1 * Math.Exp(-2 * _c * dt) - c2 * Math.Exp(-2 * _d * dt) + CurrentToTorque / _wheelVelocityToTorque * currents.Y;

            return new Vector2((float)omegaL, (float)omegaR);

            //Simplified diff eq: robot inertia moment equals Mrb*Rrb^2
            //var i = p.ToVec() / STEPS_NUMBER;
            //var exp = (float)Math.Exp(-2 * c * Dt);
            //var omegaL = omegaMax * i.X * (1 - exp) + omegaCurrent.X * exp;
            //var omegaR = omegaMax * i.Y * (1 - exp) + omegaCurrent.Y * exp;

            //var vNext = WheelsToRobotKinematics(new Vector2(omegaL, omegaR));

            //Linear approximation simplified
            //var omegaDotL = 2 * a * p.X / STEPS_NUMBER - 2 * c * omegaCurrent.X;
            //var omegaDotR = 2 * a * p.Y / STEPS_NUMBER - 2 * c * omegaCurrent.Y;

            //var vNext = WheelsToRobotKinematics(omegaCurrent + new Vector2(omegaDotL, omegaDotR) * (float)Dt);
        }

        IEnumerable<Point> GenerateWheelAccelerationGrid()
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Point>>() != null);

            return Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).
                SelectMany(wri => Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).Select(wli => new Point(wli, wri)));
        }
    }
}