using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.McLrfLocalizer;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class DwaNavigator
    {
        private readonly double _dt;
        private readonly double _robotRadius;
        
        private readonly CompositeEvaluator _compositeEvaluator;
        private readonly DwaProblemOptimizer _optimizer;

        public DwaNavigator(
            double wheelAngularAccelerationMax,
            double wheelAngularVelocityMax,

            double wheelRadius,
            double wheelBase,
            double robotRadius,
            
            RangefinderProperties rangefinderProperties,
            
            double dt)
        {
            DC.Contract.Requires(wheelAngularAccelerationMax > 0);
            DC.Contract.Requires(wheelAngularVelocityMax > 0);
            DC.Contract.Requires(wheelRadius > 0);
            DC.Contract.Requires(wheelBase > 0);
            DC.Contract.Requires(robotRadius > 0);
            DC.Contract.Requires(rangefinderProperties.MaxRange > 0);
            DC.Contract.Requires(rangefinderProperties.AngularRange > 0);
            DC.Contract.Requires(rangefinderProperties.AngularResolution > 0);
            DC.Contract.Requires(dt > 0);
            DC.Contract.Ensures(AccelerationMax.Linear > 0 && AccelerationMax.Angular > 0);
            DC.Contract.Ensures(VelocityMax.Linear > 0 && VelocityMax.Angular > 0);
            DC.Contract.Ensures(VelocitiesEvaluation != null);

            _robotRadius = robotRadius;
            RangefinderProperties = rangefinderProperties;
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

        public RangefinderProperties RangefinderProperties { get; private set; }

        public void Update(Pose pose, Pose velocity, Vector2 target, IEnumerable<float> obstacles)
        {
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(obstacles.All(d => d <= RangefinderProperties.MaxRange));
            DC.Contract.Requires(Math.Abs(velocity.Bearing) <= VelocityMax.Angular);
            DC.Contract.Requires(Math.Abs(velocity.Position.Length()) <= VelocityMax.Linear);
            DC.Contract.Ensures(VelocitiesEvaluation != null);
            DC.Contract.Ensures(VelocitiesEvaluation != DC.Contract.OldValue(VelocitiesEvaluation));

            _compositeEvaluator.EvaluatorWeights = new Dictionary<IVelocityEvaluator, double>
            {
                { new AngleToTargetEvaluator(pose, target, AccelerationMax.Angular, _dt), 0.8 },
                { new ObstaclesEvaluator(TransformMeasurements(obstacles), _robotRadius, AccelerationMax.Linear, RangefinderProperties.MaxRange), 0.1 },
                { new SpeedEvaluator(VelocityMax.Linear), 0.1 }
            };
            
            var optRes =  _optimizer.FindOptimalVelocity(velocity);
            OptimalVelocity = optRes.Item1;
            VelocitiesEvaluation = optRes.Item2;
        }

        IEnumerable<Vector2> TransformMeasurements(IEnumerable<float> obstacles)
        {
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(obstacles.All(d => d <= RangefinderProperties.MaxRange));
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Vector2>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Vector2>>().All(v => v.Length() < RangefinderProperties.MaxRange + _robotRadius));

            return obstacles.Select((d, i) => new { d, i }).Where(p => !Precision.AlmostEqualWithAbsoluteError(p.d, RangefinderProperties.MaxRange, p.d - RangefinderProperties.MaxRange, 0.01)).
                Select(p => RangefinderProperties.BeamToVectorInRobotTransformation(p.d, p.i));
        }
    }
}