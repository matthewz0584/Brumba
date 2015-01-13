using System;
using System.Collections.Generic;
using System.Linq;
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

            var obsts = RangefinderProperties.PreprocessMeasurements(obstacles).Where(ob => ob.Length() >= RobotRadius);
            var p2t = Vector2.Normalize(target - pose.Position);
            var norm = new Vector2(p2t.Y, -p2t.X);
            var borderP1 = pose.Position + norm * (float)RobotRadius * 1.5f;
            var borderP2 = pose.Position - norm * (float)RobotRadius * 1.5f;
            if (obsts.All(ob => Math.Sign(Vector2.Dot(borderP1 - (pose.Position + ob), norm)) == -Math.Sign(Vector2.Dot(borderP1 - borderP2, norm)) ||
                              Math.Sign(Vector2.Dot(borderP2 - (pose.Position + ob), norm)) == -Math.Sign(Vector2.Dot(borderP2 - borderP1, norm))))

            //_optimizer.VelocityEvaluator = new CompositeEvaluator(new Dictionary<IVelocityEvaluator, double>
            //{
            //    { new AngleToTargetEvaluator(pose, target, 1000, Dt, _velocitySpaceGenerator), 0.1 },
            //    { new DistanceToTargetEvaluator(pose, target, Dt, VelocityMax.Linear), 0.6 },
            //    { new ObstaclesEvaluator(RangefinderProperties.PreprocessMeasurements(obstacles).Where(ob => ob.Length() >= RobotRadius), RobotRadius, BreakageDeceleration, RangefinderProperties.MaxRange, Dt), 0.2 },
            //    { new SpeedEvaluator(VelocityMax.Linear), 0.1 },
            //    //{ new PersistenceEvaluator(pose, velocity, VelocityMax.Linear, Dt), 0.2 }
            //});
                _optimizer.VelocityEvaluator = new CompositeEvaluator(new Dictionary<IVelocityEvaluator, double>
            {
                { new AngleToTargetEvaluator(pose, target, 1000, Dt, _velocitySpaceGenerator), 0.7 },
                { new ObstaclesEvaluator(RangefinderProperties.PreprocessMeasurements(obstacles).Where(ob => ob.Length() >= RobotRadius), RobotRadius, BreakageDeceleration, RangefinderProperties.MaxRange, Dt), 0.2 },
                { new SpeedEvaluator(VelocityMax.Linear), 0.1 },
            });
            else
                _optimizer.VelocityEvaluator = new CompositeEvaluator(new Dictionary<IVelocityEvaluator, double>
            {
                { new DistanceToTargetEvaluator(pose, target, Dt, VelocityMax.Linear), 0.7 },
                { new ObstaclesEvaluator(RangefinderProperties.PreprocessMeasurements(obstacles).Where(ob => ob.Length() >= RobotRadius), RobotRadius, BreakageDeceleration, RangefinderProperties.MaxRange, Dt), 0.2 },
                { new SpeedEvaluator(VelocityMax.Linear), 0.1 },
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

    class DistanceToTargetEvaluator : IVelocityEvaluator
    {
        private readonly Pose _pose;
        private readonly Vector2 _target;
        private readonly double _dt;
        private readonly double _linearVelocityMax;

        public DistanceToTargetEvaluator(Pose pose, Vector2 target, double dt, double linearVelocityMax)
        {
            _pose = pose;
            _target = target;
            _dt = dt;
            _linearVelocityMax = linearVelocityMax;
        }

        public double Evaluate(Velocity v)
        {
            var newPosition = MergeSequentialPoseDeltas(_pose, ChooseMotionModel(v).PredictPoseDelta(_dt)).Position;
            return ((_target - _pose.Position).Length() - (_target - newPosition).Length()) / (2 * _linearVelocityMax * _dt) + 0.5;
        }

        IMotionModel ChooseMotionModel(Velocity v)
        {
            return Math.Abs(v.Angular) < 1e-5 ? (IMotionModel)new LineMotionModel(v.Linear) : new CircleMotionModel(v);
        }

        static Pose MergeSequentialPoseDeltas(Pose delta1, Pose delta2)
        {
            var originTransform = xMatrix.CreateRotationZ((float)delta1.Bearing) * xMatrix.CreateTranslation(new Vector3(delta1.Position, 0));
            return new Pose(Vector2.Transform(delta2.Position, originTransform), delta1.Bearing + delta2.Bearing);
        }
    }

    class PersistenceEvaluator : IVelocityEvaluator
    {
        private readonly Pose _pose;
        private readonly Pose _velocity;
        private readonly double _velocityMax;
        private readonly double _dt;

        public PersistenceEvaluator(Pose pose, Pose velocity, double velocityMax, double dt)
        {
            _pose = pose;
            _velocity = velocity;
            _velocityMax = velocityMax;
            _dt = dt;
        }

        public double Evaluate(Velocity v)
        {
            var qq = new Pose(new Vector2(), v.Angular * _dt).Direction() * (float)v.Linear;
            var ww = new Pose(new Vector2(), _velocity.Bearing * _dt).Direction();
            var originTransform1 = xMatrix.CreateRotationZ((float)_pose.Bearing);
            var originTransform2 = xMatrix.CreateRotationZ((float)(_velocity.Bearing * _dt));
            return 1 - (Vector2.Transform(qq, originTransform1) - Vector2.Transform(_velocity.Position, originTransform2)).Length() / 2 / _velocityMax;

            //var qq = new Pose(new Vector2(), v.Angular * _dt).Direction();
            //var originTransform1 = xMatrix.CreateRotationZ((float)_pose.Bearing);
            //var originTransform2 = xMatrix.CreateRotationZ((float)(_velocity.Bearing * _dt));
            //return 1 - (Vector2.Transform(qq, originTransform1) - Vector2.Transform(Vector2.Normalize(_velocity.Position), originTransform2)).Length() / 2;
        }
    }
}