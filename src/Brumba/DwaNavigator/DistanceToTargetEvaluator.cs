using System;
using Brumba.Common;
using Brumba.DiffDriveOdometry;
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
            return DiffDriveOdometryCalculator.MergeSequentialPoseDeltas(Pose, ChooseMotionModel(v).PredictPoseDeltaAsForVelocity(Dt));
        }

        public static IMotionModel ChooseMotionModel(Velocity v)
        {
            DC.Contract.Ensures(DC.Contract.Result<IMotionModel>() != null);

            return v.IsRectilinear ? (IMotionModel)new LinearMotionModel(v.Linear) : new CirclularMotionModel(v);
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