using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.McLrfLocalizer;
using Brumba.WaiterStupid;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class DwaBootstrapper
    {
        private readonly DwaProblemOptimizer _optimizer;
        private DiffDriveVelocitySpaceGenerator _velocitySpaceGenerator;

        public DwaBootstrapper(
            double robotMass, double robotInertiaMoment, double wheelRadius, double wheelBase, double robotRadius,
            double velocityMax, double breakageDeceleration, double currentToTorque, double frictionTorque,
            RangefinderProperties rangefinderProperties, double dt)
        {
            DC.Contract.Requires(breakageDeceleration > 0);
            DC.Contract.Requires(wheelRadius > 0);
            DC.Contract.Requires(wheelBase > 0);
            DC.Contract.Requires(robotRadius > 0);
            DC.Contract.Requires(rangefinderProperties.MaxRange > 0);
            DC.Contract.Requires(rangefinderProperties.AngularRange > 0);
            DC.Contract.Requires(rangefinderProperties.AngularResolution > 0);
            DC.Contract.Requires(dt > 0);
            DC.Contract.Ensures(VelocityMax.Linear > 0 && VelocityMax.Angular > 0);
            DC.Contract.Ensures(VelocitiesEvaluation != null);

            RobotRadius = robotRadius;
            BreakageDeceleration = breakageDeceleration;
            RangefinderProperties = rangefinderProperties;
            Dt = dt;

            _velocitySpaceGenerator = new DiffDriveVelocitySpaceGenerator(robotMass, robotInertiaMoment, wheelRadius, wheelBase, velocityMax, currentToTorque, frictionTorque, dt);
            
            VelocitiesEvaluation = new DenseMatrix(2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1);

            VelocityMax = new Velocity(velocityMax,
                    _velocitySpaceGenerator.WheelsToRobotKinematics(new Vector2(-1, 1) * (float)(velocityMax / wheelRadius)).Angular);

            _optimizer = new DwaProblemOptimizer(_velocitySpaceGenerator, VelocityMax);
        }

        public double BreakageDeceleration { get; private set; }
        public double Dt { get; private set; }
        public double RobotRadius { get; private set; }
        public RangefinderProperties RangefinderProperties { get; private set; }

        public Velocity VelocityMax { get; private set; }

        public VelocityAcceleration OptimalVelocity { get; private set; }
        public DenseMatrix VelocitiesEvaluation { get; private set; }

        public void Update(Pose pose, Pose velocity, Vector2 target, IEnumerable<float> obstacles)
        {
            DC.Contract.Requires(obstacles != null);
            DC.Contract.Requires(obstacles.All(d => d <= RangefinderProperties.MaxRange));
            DC.Contract.Requires(Math.Abs(velocity.Bearing) <= VelocityMax.Angular);
            DC.Contract.Requires(Math.Abs(velocity.Position.Length()) <= VelocityMax.Linear);
            DC.Contract.Ensures(VelocitiesEvaluation != null);
            DC.Contract.Ensures(VelocitiesEvaluation != DC.Contract.OldValue(VelocitiesEvaluation));

            _optimizer.VelocityEvaluator = new CompositeEvaluator(new Dictionary<IVelocityEvaluator, double>
            {
                { new AngleToTargetEvaluator(pose, target, _velocitySpaceGenerator.WheelsToRobotKinematics(new Vector2(-(float)25, (float)25)).Angular, Dt), 0.7 },
                { new ObstaclesEvaluator(RangefinderProperties.PreprocessMeasurements(obstacles).Where(ob => ob.Length() >= RobotRadius), RobotRadius, BreakageDeceleration, RangefinderProperties.MaxRange, Dt), 0.2 },
                { new SpeedEvaluator(VelocityMax.Linear), 0.1 }
            });

            var optRes = _optimizer.FindOptimalVelocity(SubjectiveVelocity(pose, velocity));
            OptimalVelocity = optRes.Item1;
            VelocitiesEvaluation = optRes.Item2;

            //_optimizer.VelocityEvaluator = new ObstaclesEvaluator(
            //        RangefinderProperties.PreprocessMeasurements(obstacles).Where(ob => ob.Length() >= _robotRadius),
            //        _robotRadius, BreakageDeceleration, RangefinderProperties.MaxRange);

            //var optRes = _optimizer.FindOptimalVelocity(velocity);
            //VelocitiesEvaluation = optRes.Item2;
            //OptimalVelocity = optRes.Item1;
        }

        public static Velocity SubjectiveVelocity(Pose pose, Pose velocity)
        {
            return new Velocity(Vector2.Dot(pose.Direction(), velocity.Position), velocity.Bearing);
        }
    }
}