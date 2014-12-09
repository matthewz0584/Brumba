using System;
using Brumba.Utils;
using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class AngleToTargetEvaluator : IVelocityEvaluator
    {
        public AngleToTargetEvaluator(Pose pose, Vector2 target, double maxAngularAcceleration, double dt)
        {
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

            return 1 - GetAngleToTarget(MergeSequentialPoseDeltas(Pose, PredictPoseDelta(v))) / MathHelper.Pi;
        }

        public Pose PredictPoseDelta(Velocity v)
        {
            DC.Contract.Requires(v.Linear >= 0);

            var angularVelocity2 = CalculateAngularVelocityAfterDeceleration(v.Angular);

            return MergeSequentialPoseDeltas(
                ChooseMotionModel(v).PredictPoseDelta(Dt),
                ChooseMotionModel(new Velocity(v.Linear, angularVelocity2)).PredictPoseDelta(Dt));
        }

        public float GetAngleToTarget(Pose pose)
        {
            DC.Contract.Ensures(DC.Contract.Result<float>() >= 0 && DC.Contract.Result<float>() <= MathHelper.Pi);

            return MathHelper2.AngleBetween(Target - pose.Position,
                new Vector2((float) Math.Cos(pose.Bearing), (float) Math.Sin(pose.Bearing)));
        }

        IMotionModel ChooseMotionModel(Velocity v)
        {
            return Math.Abs(v.Angular) < 1e-5 ? (IMotionModel)new LineMotionModel(v.Linear) : new CircleMotionModel(v);
        }

        double CalculateAngularVelocityAfterDeceleration(double angularVelocity)
        {
            DC.Contract.Ensures(angularVelocity == 0 && DC.Contract.Result<double>() == 0 ||
                                angularVelocity != 0 && Math.Abs(DC.Contract.Result<double>()) < Math.Abs(angularVelocity));

            var neededDeceleration = - angularVelocity/Dt;
            var angularVelocityDecelerated = Math.Abs(neededDeceleration) > MaxAngularAcceleration
                ? angularVelocity - Math.Sign(angularVelocity) * MaxAngularAcceleration * Dt : 0;
            return angularVelocityDecelerated;
        }

        static Pose MergeSequentialPoseDeltas(Pose delta1, Pose delta2)
        {
            var originTransform = Matrix.CreateRotationZ((float)delta1.Bearing) * Matrix.CreateTranslation(new Vector3(delta1.Position, 0));
            return new Pose(Vector2.Transform(delta2.Position, originTransform), delta1.Bearing + delta2.Bearing);
        }
    }
}