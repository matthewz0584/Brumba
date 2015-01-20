using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.DiffDriveOdometry;
using Brumba.McLrfLocalizer;
using Brumba.WaiterStupid;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;
using xMatrix = Microsoft.Xna.Framework.Matrix;

namespace Brumba.DwaNavigator
{
    public class DwaBootstrapper
    {
        private readonly DwaProblemOptimizer _optimizer;
        private DiffDriveVelocitySpaceGenerator _velocitySpaceGenerator;

        public DwaBootstrapper(
            double robotMass, double robotInertiaMoment, double wheelRadius, double wheelBase, double robotRadius,
            double velocityMax, double breakageDeceleration, double currentToTorque, double frictionTorque,
            RangefinderProperties rangefinderProperties,
            double laneWidthCoef, double dt)
        {
            DC.Contract.Requires(breakageDeceleration > 0);
            DC.Contract.Requires(wheelRadius > 0);
            DC.Contract.Requires(wheelBase > 0);
            DC.Contract.Requires(robotRadius > 0);
            DC.Contract.Requires(rangefinderProperties.MaxRange > 0);
            DC.Contract.Requires(rangefinderProperties.AngularRange > 0);
            DC.Contract.Requires(rangefinderProperties.AngularResolution > 0);
            DC.Contract.Requires(laneWidthCoef >= 1);
            DC.Contract.Requires(dt > 0);
            DC.Contract.Ensures(VelocitiesEvaluation != null);

            RobotRadius = robotRadius;
            VelocityMax = velocityMax;
            BreakageDeceleration = breakageDeceleration;
            RangefinderProperties = rangefinderProperties;
            LaneWidthCoef = laneWidthCoef;
            Dt = dt;

            _velocitySpaceGenerator = new DiffDriveVelocitySpaceGenerator(robotMass, robotInertiaMoment, wheelRadius, wheelBase, velocityMax, currentToTorque, frictionTorque, dt);
            
            _optimizer = new DwaProblemOptimizer(_velocitySpaceGenerator, new Velocity(velocityMax,
                    _velocitySpaceGenerator.WheelsToRobotKinematics(new Vector2(-1, 1) * (float)(velocityMax / wheelRadius)).Angular));

            VelocitiesEvaluation = new DenseMatrix(2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1);
        }

        public double RobotRadius { get; private set; }
        public double VelocityMax { get; private set; }
        public double BreakageDeceleration { get; private set; }
        public RangefinderProperties RangefinderProperties { get; private set; }
        
        public double LaneWidthCoef { get; private set; }
        public double Dt { get; private set; }

        public VelocityAcceleration OptimalVelocity { get; private set; }
        public DenseMatrix VelocitiesEvaluation { get; private set; }

        public void Update(Pose pose, Pose velocity, Vector2 target, IEnumerable<float> obstacles)
        {
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(obstacles.All(d => d <= RangefinderProperties.MaxRange));
            DC.Contract.Requires(Math.Abs(velocity.Position.Length()) <= VelocityMax);
            DC.Contract.Ensures(VelocitiesEvaluation != null);
            DC.Contract.Ensures(VelocitiesEvaluation != DC.Contract.OldValue(VelocitiesEvaluation));

            var obstaclesEvaluator = new ObstaclesEvaluator(RangefinderProperties.PreprocessMeasurements(obstacles).Where(ob => ob.Length() >= RobotRadius), RobotRadius, BreakageDeceleration, LaneWidthCoef, Dt);

            _optimizer.VelocityEvaluator = new CompositeEvaluator(new Dictionary<IVelocityEvaluator, double>
                {
                    { obstaclesEvaluator.IsStraightPathClear(Vector2.Transform(target - pose.Position, xMatrix.CreateRotationZ(-(float)pose.Bearing))) ? (IVelocityEvaluator)
                        new AngleToTargetEvaluator(pose, target, Dt, _velocitySpaceGenerator) :
                        new DistanceToTargetEvaluator(pose, target, VelocityMax, Dt), 0.7 },
                    { obstaclesEvaluator, 0.2 },
                    { new SpeedEvaluator(VelocityMax), 0.1 },
                });

            var optRes = _optimizer.FindOptimalVelocity(SubjectiveVelocity(pose, velocity));
            OptimalVelocity = optRes.Item1;
            VelocitiesEvaluation = optRes.Item2;

            //_optimizer.VelocityEvaluator = new ObstaclesEvaluator(RangefinderProperties.PreprocessMeasurements(obstacles).Where(ob => ob.Length() >= RobotRadius), RobotRadius, BreakageDeceleration, RangefinderProperties.MaxRange, Dt);
            //_optimizer.VelocityEvaluator = new AngleToTargetEvaluator(pose, target, 1000, Dt, _velocitySpaceGenerator);
            //optRes = _optimizer.FindOptimalVelocity(SubjectiveVelocity(pose, velocity));
            //VelocitiesEvaluation = optRes.Item2;
        }

        public static Velocity SubjectiveVelocity(Pose pose, Pose velocity)
        {
            return new Velocity(Vector2.Dot(pose.Direction(), velocity.Position), velocity.Bearing);
        }
    }
}