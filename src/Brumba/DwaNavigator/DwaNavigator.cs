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
        private readonly double _wheelAngularVelocityMax;
        private readonly double _rangefinderMaxRange;
        
        private readonly CompositeEvaluator _compositeEvaluator;
        private readonly DwaProblemOptimizer _optimizer;
        private readonly DynamicDiamondGenerator _dynamicDiamondGenerator;

        public DwaNavigator(
            double wheelAngularAccelerationMax,
            double wheelAngularVelocityMax,

            double wheelRadius,
            double wheelBase,
            double robotRadius,
            
            double rangefinderMaxRange,
            
            double dt)
        {
            DC.Contract.Ensures(AccelerationMax.Linear > 0 && AccelerationMax.Angular > 0);

            _robotRadius = robotRadius;
            _rangefinderMaxRange = rangefinderMaxRange;
            _dt = dt;

            _dynamicDiamondGenerator = new DynamicDiamondGenerator(wheelAngularAccelerationMax, wheelRadius, wheelBase, dt);
            
            AccelerationMax = new Velocity(
                    _dynamicDiamondGenerator.WheelsToRobotKinematics(new Vector2((float)wheelAngularAccelerationMax, (float)wheelAngularAccelerationMax)).Linear,
                    _dynamicDiamondGenerator.WheelsToRobotKinematics(new Vector2(-(float)wheelAngularAccelerationMax, (float)wheelAngularAccelerationMax)).Angular);

            VelocityMax = new Velocity(
                    _dynamicDiamondGenerator.WheelsToRobotKinematics(new Vector2((float)wheelAngularVelocityMax, (float)wheelAngularVelocityMax)).Linear,
                    _dynamicDiamondGenerator.WheelsToRobotKinematics(new Vector2(-(float)wheelAngularVelocityMax, (float)wheelAngularVelocityMax)).Angular);

            _compositeEvaluator = new CompositeEvaluator();
            _optimizer = new DwaProblemOptimizer(_dynamicDiamondGenerator, _compositeEvaluator, VelocityMax);
        }

        public Velocity AccelerationMax { get; private set; }
        public Velocity VelocityMax { get; private set; }

        public VelocityAcceleration OptimalVelocity { get; private set; }
        public DenseMatrix VelocitiesEvaluation { get; private set; }

        public void Update(Pose pose, Pose velocity, Vector2 target, IEnumerable<Vector2> obstacles)
        {
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