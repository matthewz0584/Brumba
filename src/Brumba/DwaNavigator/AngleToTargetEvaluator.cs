using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class AngleToTargetEvaluator : IVelocityEvaluator
    {
        private readonly DiffDriveVelocitySpaceGenerator _ddvsg;

        public AngleToTargetEvaluator(Pose pose, Vector2 target, double maxAngularAcceleration, double dt, DiffDriveVelocitySpaceGenerator ddvsg)
        {
            _ddvsg = ddvsg;
            DC.Contract.Requires(maxAngularAcceleration > 0);
            DC.Contract.Requires(dt > 0);

            Pose = pose;
            Target = target;
            MaxAngularAcceleration = maxAngularAcceleration;
            Dt = dt;
        }

        public Pose Pose { get; private set; }
        public Vector2 Target { get; private set; }
        public double MaxAngularAcceleration { get; private set; }
        public double Dt { get; private set; }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Assert(v.Linear >= 0);

            var qq = 1 - GetAngleToTarget(MergeSequentialPoseDeltas(Pose, PredictPoseDelta(v)))/Constants.Pi;
            return qq * qq;
        }

        public Pose PredictPoseDelta(Velocity v)
        {
            DC.Contract.Requires(v.Linear >= 0);

            var v2 = CalculateVelocityAfterAngularDeceleration(v);

            return MergeSequentialPoseDeltas(
                ChooseMotionModel(v).PredictPoseDelta(Dt),
                ChooseMotionModel(v2).PredictPoseDelta(Dt));
        }

        public double GetAngleToTarget(Pose pose)
        {
            DC.Contract.Ensures(DC.Contract.Result<double>() >= 0 && DC.Contract.Result<double>() <= Constants.Pi);

            return MathHelper2.AngleBetween(Target - pose.Position, pose.Direction());
        }

        IMotionModel ChooseMotionModel(Velocity v)
        {
            return Math.Abs(v.Angular) < 1e-5 ? (IMotionModel)new LineMotionModel(v.Linear) : new CircleMotionModel(v);
        }

        Velocity CalculateVelocityAfterAngularDeceleration(Velocity v)
        {
            var v2 = _ddvsg.WheelsToRobotKinematics(_ddvsg.PredictWheelVelocities(
                _ddvsg.RobotKinematicsToWheels(v), new Vector2(1, -1) * Math.Sign(v.Angular), Dt/2));

            return Math.Sign(v2.Angular) == Math.Sign(v.Angular) ? v2 : new Velocity(v.Linear, 0);
        }

        static Pose MergeSequentialPoseDeltas(Pose delta1, Pose delta2)
        {
            var originTransform = Matrix.CreateRotationZ((float)delta1.Bearing) * Matrix.CreateTranslation(new Vector3(delta1.Position, 0));
            return new Pose(Vector2.Transform(delta2.Position, originTransform), delta1.Bearing + delta2.Bearing);
        }
    }

    public class AngleToTargetEvaluator2 : IVelocityEvaluator
    {
        private static List<double> _bearings = new List<double>();

        private readonly DiffDriveVelocitySpaceGenerator _ddvsg;
        private Vector2 _anotherTarget;

        public AngleToTargetEvaluator2(Pose pose, Vector2 target, double maxAngularAcceleration, double dt, DiffDriveVelocitySpaceGenerator ddvsg)
        {
            _ddvsg = ddvsg;
            DC.Contract.Requires(maxAngularAcceleration > 0);
            DC.Contract.Requires(dt > 0);

            Pose = pose;
            Target = target;
            MaxAngularAcceleration = maxAngularAcceleration;
            Dt = dt;

            _bearings = pose.Bearing.ToMinAbsValueAngle().AsCol().Concat(_bearings).Take(2).ToList();
            _anotherTarget = Pose.Position + new Pose(new Vector2(), _bearings.Average()).Direction() * (Target - Pose.Position).Length();
        }

        public Pose Pose { get; private set; }
        public Vector2 Target { get; private set; }
        public double MaxAngularAcceleration { get; private set; }
        public double Dt { get; private set; }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Assert(v.Linear >= 0);

            var qq = 1 - GetAngleToTarget(MergeSequentialPoseDeltas(Pose, PredictPoseDelta(v))) / Constants.Pi;
            return qq;
        }

        public Pose PredictPoseDelta(Velocity v)
        {
            DC.Contract.Requires(v.Linear >= 0);

            var v2 = CalculateVelocityAfterAngularDeceleration(v);

            return MergeSequentialPoseDeltas(
                ChooseMotionModel(v).PredictPoseDelta(Dt),
                ChooseMotionModel(v2).PredictPoseDelta(Dt));
        }

        public double GetAngleToTarget(Pose pose)
        {
            DC.Contract.Ensures(DC.Contract.Result<double>() >= 0 && DC.Contract.Result<double>() <= Constants.Pi);

            return MathHelper2.AngleBetween((Target + _anotherTarget) / 2 - pose.Position, pose.Direction());
        }

        IMotionModel ChooseMotionModel(Velocity v)
        {
            return Math.Abs(v.Angular) < 1e-5 ? (IMotionModel)new LineMotionModel(v.Linear) : new CircleMotionModel(v);
        }

        Velocity CalculateVelocityAfterAngularDeceleration(Velocity v)
        {
            var v2 = _ddvsg.WheelsToRobotKinematics(_ddvsg.PredictWheelVelocities(
                _ddvsg.RobotKinematicsToWheels(v), new Vector2(1, -1) * Math.Sign(v.Angular), Dt / 2));

            return Math.Sign(v2.Angular) == Math.Sign(v.Angular) ? v2 : new Velocity(v.Linear, 0);
        }

        static Pose MergeSequentialPoseDeltas(Pose delta1, Pose delta2)
        {
            var originTransform = Matrix.CreateRotationZ((float)delta1.Bearing) * Matrix.CreateTranslation(new Vector3(delta1.Position, 0));
            return new Pose(Vector2.Transform(delta2.Position, originTransform), delta1.Bearing + delta2.Bearing);
        }
    }

}