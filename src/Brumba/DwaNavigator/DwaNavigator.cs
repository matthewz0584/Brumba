using System.Collections.Generic;
using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;

namespace Brumba.DwaNavigator
{
    public class DwaNavigator
    {
        private readonly double _dt;
        private readonly double _robotRadius;
        private readonly double _linearDecelerationMax;
        private readonly double _maxRange;
        private readonly double _maxSpeed;
        
        private readonly CompositeEvaluator _compositeEvaluator;
        private readonly DwaProblemOptimizer _optimizer;
        private readonly DynamicDiamondGenerator _dynamicDiamondGenerator;

        public DwaNavigator(double wheelAngularAccelerationMax, double wheelRadius, double wheelBase, double dt, double robotRadius, double linearDecelerationMax, double maxRange, double maxSpeed)
        {
            _dt = dt;
            _robotRadius = robotRadius;
            _linearDecelerationMax = linearDecelerationMax;
            _maxRange = maxRange;
            _maxSpeed = maxSpeed;
            _compositeEvaluator = new CompositeEvaluator();
            _dynamicDiamondGenerator = new DynamicDiamondGenerator(wheelAngularAccelerationMax, wheelRadius, wheelBase, dt);
            _optimizer = new DwaProblemOptimizer(
                _dynamicDiamondGenerator,
                _compositeEvaluator);
        }

        public Vector2 Cycle(Pose pose, Pose velocity, Vector2 target, IEnumerable<Vector2> obstacles)
        {
            _compositeEvaluator.EvaluatorWeights = new Dictionary<IVelocityEvaluator, double>
            {
                { new AngleToTargetEvaluator(pose, target, _dynamicDiamondGenerator.AccelerationMax.Angular, _dt), 0.8 },
                { new ObstaclesEvaluator(obstacles, _robotRadius, _linearDecelerationMax, _maxRange), 0.1 },
                { new SpeedEvaluator(_maxSpeed), 0.1 }
            };
            
            return _optimizer.FindOptimalVelocity(velocity);
        }
    }
}