using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    [DC.ContractClassAttribute(typeof(VelocitySpaceGeneratorContract))]
    public interface IVelocitySpaceGenerator
    {
        VelocityAcceleration[,] Generate(Velocity center);
    }

    [DC.ContractClassForAttribute(typeof(IVelocitySpaceGenerator))]
    abstract class VelocitySpaceGeneratorContract : IVelocitySpaceGenerator
    {
        public VelocityAcceleration[,] Generate(Velocity center)
        {
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>() != null);

            return default(VelocityAcceleration[,]);
        }
    }

    public class DiffDriveVelocitySpaceGenerator : IVelocitySpaceGenerator
    {
        private readonly double _currentToTorque;
        private double _vMax;
        private double _a;
        private double _b;
        private double _c;
        private double _d;
        private double _wheelVelocityToTorque;
        public const int STEPS_NUMBER = 10;

        //public DiffDriveVelocitySpaceGenerator(double wheelRadius, double wheelBase, double dt)
        public DiffDriveVelocitySpaceGenerator(
                double robotMass, double robotInertiaMoment, double velocityMax, 
                double wheelRadius, double wheelBase, 
                double currentToTorque, double dt)
        {
            DC.Contract.Requires(wheelRadius > 0);
            DC.Contract.Requires(wheelBase > 0);
            DC.Contract.Requires(dt > 0);

            WheelRadius = wheelRadius;
            WheelBase = wheelBase;

            _currentToTorque = currentToTorque;
            Dt = dt;

            _wheelVelocityToTorque = currentToTorque / (velocityMax / WheelRadius);
            var massCoef = 1 / (robotMass * WheelRadius * WheelRadius);
            var momentCoef = WheelBase / 2 * WheelBase / 2 / (robotInertiaMoment * WheelRadius * WheelRadius);
            _a = currentToTorque * massCoef;
            _b = currentToTorque * momentCoef;
            _c = _wheelVelocityToTorque * massCoef;
            _d = _wheelVelocityToTorque * momentCoef;
        }

        public double WheelRadius { get; private set; }
        public double WheelBase { get; private set; }

        public double Dt { get; private set; }

        public VelocityAcceleration[,] Generate(Velocity diamondCenter)
        {
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>() != null);
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>().GetLength(0) == 2 * STEPS_NUMBER + 1);
            DC.Contract.Ensures(DC.Contract.Result<VelocityAcceleration[,]>().GetLength(1) == 2 * STEPS_NUMBER + 1);

            var velocitySpace = new VelocityAcceleration[2 * STEPS_NUMBER + 1, 2 * STEPS_NUMBER + 1];
            foreach (var p in GenerateWheelAccelerationGrid())
                velocitySpace[p.X + STEPS_NUMBER, p.Y + STEPS_NUMBER] = new VelocityAcceleration(
                        WheelsToRobotKinematics(PredictWheelVelocities(RobotKinematicsToWheels(diamondCenter), p.ToVec() / STEPS_NUMBER)),
                        p.ToVec() / STEPS_NUMBER);
            return velocitySpace;
        }

        public Vector2 PredictWheelVelocities(Vector2 omegaCurrent, Vector2 currents)
        {
            //Linear approx
            var iSum = currents.X + currents.Y;
            var iDiff = currents.X - currents.Y;
            var omegaSum = omegaCurrent.X + omegaCurrent.Y;
            var omegaDiff = omegaCurrent.X - omegaCurrent.Y;
            //var omegaDotL = _a * iSum + _b * iDiff - _c * omegaSum - _d * omegaDiff;
            //var omegaDotR = _a * iSum - _b * iDiff - _c * omegaSum + _d * omegaDiff;

            //return omegaCurrent + new Vector2((float)omegaDotL, (float)omegaDotR) * (float)Dt;

            //Correct diff eq
            var c1 = (omegaSum - _currentToTorque / _wheelVelocityToTorque * iSum) / 2;
            var c2 = (omegaDiff - _currentToTorque / _wheelVelocityToTorque * iDiff) / 2;

            var omegaL = c1 * Math.Exp(-2 * _c * Dt) + c2 * Math.Exp(-2 * _d * Dt) + _currentToTorque / _wheelVelocityToTorque * currents.X;
            var omegaR = c1 * Math.Exp(-2 * _c * Dt) - c2 * Math.Exp(-2 * _d * Dt) + _currentToTorque / _wheelVelocityToTorque * currents.Y;

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

        public Velocity WheelsToRobotKinematics(Vector2 wheelsValues)
        {
            return new Velocity(WheelRadius / 2 * (wheelsValues.Y + wheelsValues.X), WheelRadius / WheelBase * (wheelsValues.Y - wheelsValues.X));
        }

        public Vector2 RobotKinematicsToWheels(Velocity v)
        {
            return new Vector2((float)(v.Linear * 2 - v.Angular * WheelBase), (float)(v.Linear * 2 + v.Angular * WheelBase)) / 2 / (float)WheelRadius;
        }

        IEnumerable<Point> GenerateWheelAccelerationGrid()
        {
            return Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).
                SelectMany(wri => Enumerable.Range(-STEPS_NUMBER, 2 * STEPS_NUMBER + 1).Select(wli => new Point(wli, wri)));
        }
    }
}