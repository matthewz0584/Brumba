using System;
using System.Collections.Generic;
using Brumba.WaiterStupid;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class DwaNavigator
    {
        private readonly double _dt;
        private readonly double _robotRadius;
        private readonly double _rangefinderMaxRange;
        
        private readonly CompositeEvaluator _compositeEvaluator;
        private readonly DwaProblemOptimizer _optimizer;

        public DwaNavigator(
            double wheelAngularAccelerationMax,
            double wheelAngularVelocityMax,

            double wheelRadius,
            double wheelBase,
            double robotRadius,
            
            double rangefinderMaxRange,
            
            double dt)
        {
            DC.Contract.Requires(wheelAngularAccelerationMax > 0);
            DC.Contract.Requires(wheelAngularVelocityMax > 0);
            DC.Contract.Requires(wheelRadius > 0);
            DC.Contract.Requires(wheelBase > 0);
            DC.Contract.Requires(robotRadius >= wheelBase);
            DC.Contract.Requires(rangefinderMaxRange > 0);
            DC.Contract.Requires(dt > 0);
            DC.Contract.Ensures(AccelerationMax.Linear > 0 && AccelerationMax.Angular > 0);
            DC.Contract.Ensures(VelocityMax.Linear > 0 && VelocityMax.Angular > 0);
            DC.Contract.Ensures(VelocitiesEvaluation != null);

            _robotRadius = robotRadius;
            _rangefinderMaxRange = rangefinderMaxRange;
            _dt = dt;

            var dynamicDiamondGenerator = new DynamicDiamondGenerator(wheelAngularAccelerationMax, wheelRadius, wheelBase, dt);
            
            AccelerationMax = new Velocity(
                    dynamicDiamondGenerator.WheelsToRobotKinematics(new Vector2((float)wheelAngularAccelerationMax, (float)wheelAngularAccelerationMax)).Linear,
                    dynamicDiamondGenerator.WheelsToRobotKinematics(new Vector2(-(float)wheelAngularAccelerationMax, (float)wheelAngularAccelerationMax)).Angular);

            VelocityMax = new Velocity(
                    dynamicDiamondGenerator.WheelsToRobotKinematics(new Vector2((float)wheelAngularVelocityMax, (float)wheelAngularVelocityMax)).Linear,
                    dynamicDiamondGenerator.WheelsToRobotKinematics(new Vector2(-(float)wheelAngularVelocityMax, (float)wheelAngularVelocityMax)).Angular);

            VelocitiesEvaluation = new DenseMatrix(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1);

            _compositeEvaluator = new CompositeEvaluator();
            _optimizer = new DwaProblemOptimizer(dynamicDiamondGenerator, _compositeEvaluator, VelocityMax);
        }

        public Velocity AccelerationMax { get; private set; }
        public Velocity VelocityMax { get; private set; }

        public VelocityAcceleration OptimalVelocity { get; private set; }
        public DenseMatrix VelocitiesEvaluation { get; private set; }

        public void Update(Pose pose, Pose velocity, Vector2 target, IEnumerable<Vector2> obstacles)
        {
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(Math.Abs(velocity.Bearing) <= VelocityMax.Angular);
            DC.Contract.Requires(Math.Abs(velocity.Position.Length()) <= VelocityMax.Linear);
            DC.Contract.Ensures(VelocitiesEvaluation != null);
            DC.Contract.Ensures(VelocitiesEvaluation != DC.Contract.OldValue(VelocitiesEvaluation));

            _compositeEvaluator.EvaluatorWeights = new Dictionary<IVelocityEvaluator, double>
            {
                { new AngleToTargetEvaluator(pose, target, AccelerationMax.Angular, _dt), 0.8 },
                { new ObstaclesEvaluator(obstacles, _robotRadius, AccelerationMax.Linear, _rangefinderMaxRange), 0.1 },
                { new SpeedEvaluator(VelocityMax.Linear), 0.1 }
            };
            
            var optRes =  _optimizer.FindOptimalVelocity(velocity);
            OptimalVelocity = optRes.Item1;
            VelocitiesEvaluation = optRes.Item2;
        }
    }
}