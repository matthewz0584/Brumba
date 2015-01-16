using System;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class AngleToTargetEvaluator : NextPoseEvaluator, IVelocityEvaluator
    {
        public AngleToTargetEvaluator(Pose pose, Vector2 target, double dt, IVelocityPredictor velocityPredictor)
            : base(pose, dt)
        {
            DC.Contract.Requires(dt > 0);
            DC.Contract.Requires(velocityPredictor != null);

            Target = target;
            VelocityPredictor = velocityPredictor;
        }

        public Vector2 Target { get; private set; }
        public IVelocityPredictor VelocityPredictor { get; private set; }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Assert(v.Linear >= 0);

            return 1 - GetAngleToTarget(MergeSequentialPoseDeltas(PredictNextPose(v), ChooseMotionModel(CalculateVelocityAfterAngularDeceleration(v)).PredictPoseDelta(Dt))) / Constants.Pi;
        }

        public double GetAngleToTarget(Pose pose)
        {
            DC.Contract.Ensures(DC.Contract.Result<double>() >= 0 && DC.Contract.Result<double>() <= Constants.Pi);

            return MathHelper2.AngleBetween(Target - pose.Position, pose.Direction());
        }

        public Velocity CalculateVelocityAfterAngularDeceleration(Velocity v)
        {
            if (v.Angular == 0) return v;

            var deceleratedV = VelocityPredictor.PredictVelocity(v, new Vector2(1, -1) * Math.Sign(v.Angular), Dt/2);

            return Math.Sign(deceleratedV.Angular) == Math.Sign(v.Angular) ? deceleratedV : new Velocity(v.Linear, 0);
        }
    }
}