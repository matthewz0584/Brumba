using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Robotics.Simulation.Engine;
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

            var omegaCurrent = RobotKinematicsToWheels(diamondCenter);

            var alpha = 1f;
            var vMax = 1.5f;
            var omegaMax = vMax / (float)WheelRadius;
            //var beta = alpha / wMax;
            //var robotRadius = 0.226f;
            var robotMass = 9.1f;
            //var robotInertiaMoment = robotMass * robotRadius * robotRadius;
            var a = alpha / (robotMass * (float)WheelRadius * (float)WheelRadius);
            //var b = alpha * robotRadius * robotRadius / (robotInertiaMoment * (float)WheelRadius * (float)WheelRadius);
            var c = a / omegaMax;
            //var d = beta * robotRadius * robotRadius / (robotInertiaMoment * (float)WheelRadius * (float)WheelRadius);

            var velocitySpace = new VelocityAcceleration[2 * STEPS_NUMBER + 1, 2 * STEPS_NUMBER + 1];
            foreach (var p in GenerateWheelAccelerationGrid())
            {
                //Linear approx
                //var iSum = (float)(p.X + p.Y) / STEPS_NUMBER;
                //var iDiff = (float)(p.X - p.Y) / STEPS_NUMBER;
                //var omegaSum = omegaCurrent.X + omegaCurrent.Y;
                //var omegaDiff = omegaCurrent.X - omegaCurrent.Y;
                //var omegaDotL = a * iSum + b * iDiff - c * omegaSum + d * omegaDiff;
                //var omegaDotR = a * iSum - b * iDiff - c * omegaSum - d * omegaDiff;

                //var vNext = WheelsToRobotKinematics(omegaCurrent + new Vector2(omegaDotL, omegaDotR) * (float)Dt);
                //velocitySpace[p.X + STEPS_NUMBER, p.Y + STEPS_NUMBER] = new VelocityAcceleration(vNext, p.ToVec() / STEPS_NUMBER);

                ////Incorrect diff eq
                //var iSum = (float)(p.X + p.Y) / STEPS_NUMBER;
                //var iDiff = (float)(p.X - p.Y) / STEPS_NUMBER;
                //var omegaSum = omegaCurrent.X + omegaCurrent.Y;
                //var omegaDiff = omegaCurrent.X - omegaCurrent.Y;
                //var A = a * iSum;
                //var B = b * iDiff;
                //var C = c * omegaSum;
                //var D = d * omegaDiff;
                //var c1 = (omegaDiff - B / D) / 2;
                //var c2 = (omegaSum - A / C) / 2;
                //var g1 = (A * D - B * C) / (2 * D * C);
                //var g2 = ((D - C) * g1 + A + B) / (D + C);

                //var omegaL = (float)(c1 * Math.Exp(2 * D * Dt) + c2 * Math.Exp(-2 * C * Dt) + g1);
                //var omegaR = (float)(-c1 * Math.Exp(2 * D * Dt) + c2 * Math.Exp(-2 * C * Dt) + g2);

                //var vNext = WheelsToRobotKinematics(new Vector2(omegaL, omegaR));
                //velocitySpace[p.X + STEPS_NUMBER, p.Y + STEPS_NUMBER] = new VelocityAcceleration(vNext, p.ToVec() / STEPS_NUMBER);

                //Simplified diff eq: robot inertia moment equals Mrb*Rrb^2
                //var i = p.ToVec() / STEPS_NUMBER;
                //var exp = (float)Math.Exp(-2 * c * Dt);
                //var omegaL = omegaMax * i.X * (1 - exp) + omegaCurrent.X * exp;
                //var omegaR = omegaMax * i.Y * (1 - exp) + omegaCurrent.Y * exp;

                //var vNext = WheelsToRobotKinematics(new Vector2(omegaL, omegaR));
                //velocitySpace[p.X + STEPS_NUMBER, p.Y + STEPS_NUMBER] = new VelocityAcceleration(vNext, p.ToVec() / STEPS_NUMBER);

                //Linear approximation simplified
                var omegaDotL = 2 * a * p.X / STEPS_NUMBER - 2 * c * omegaCurrent.X;
                var omegaDotR = 2 * a * p.Y / STEPS_NUMBER - 2 * c * omegaCurrent.Y;

                var vNext = WheelsToRobotKinematics(omegaCurrent + new Vector2(omegaDotL, omegaDotR) * (float)Dt);
                velocitySpace[p.X + STEPS_NUMBER, p.Y + STEPS_NUMBER] = new VelocityAcceleration(vNext, p.ToVec() / STEPS_NUMBER);
            }
            return velocitySpace;
        }

        Vector2 RobotKinematicsToWheels(Velocity v)
        {
            return new Vector2((float)(v.Linear * 2 - v.Angular * WheelBase), (float)(v.Linear * 2 + v.Angular * WheelBase)) / 2 / (float)WheelRadius;
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