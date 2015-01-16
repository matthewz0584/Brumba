using System;
using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class NextPoseEvaluator
    {
        public NextPoseEvaluator(Pose pose, double dt)
        {
            DC.Contract.Requires(dt > 0);

            Pose = pose;
            Dt = dt;
        }

        public Pose Pose { get; private set; }
        public double Dt { get; private set; }

        public Pose PredictNextPose(Velocity v)
        {
            return MergeSequentialPoseDeltas(Pose, ChooseMotionModel(v).PredictPoseDelta(Dt));
        }

        public static IMotionModel ChooseMotionModel(Velocity v)
        {
            DC.Contract.Ensures(DC.Contract.Result<IMotionModel>() != null);

            return v.IsRectilinear ? (IMotionModel)new LinearMotionModel(v.Linear) : new CirclularMotionModel(v);
        }

        public static Pose MergeSequentialPoseDeltas(Pose delta1, Pose delta2)
        {
            var originTransform = Matrix.CreateRotationZ((float)delta1.Bearing) * Matrix.CreateTranslation(new Vector3(delta1.Position, 0));
            return new Pose(Vector2.Transform(delta2.Position, originTransform), delta1.Bearing + delta2.Bearing);
        }
    }

    public class DistanceToTargetEvaluator : NextPoseEvaluator, IVelocityEvaluator
    {
        public DistanceToTargetEvaluator(Pose pose, Vector2 target, double linearVelocityMax, double dt)
            : base(pose, dt)
        {
            DC.Contract.Requires(linearVelocityMax > 0);
            DC.Contract.Requires(dt > 0);

            Target = target;
            LinearVelocityMax = linearVelocityMax;
        }

        public Vector2 Target { get; private set; }
        public double LinearVelocityMax { get; private set; }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Requires(Math.Abs(v.Linear) <= LinearVelocityMax);

            return ((Target - Pose.Position).Length() - (Target - PredictNextPose(v).Position).Length()) / (2 * LinearVelocityMax * Dt) + 0.5;
        }
    }
}