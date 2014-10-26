using System;
using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class AngleToTargetPredictor
    {
        private readonly Vector2 _target;
        private readonly double _maxAngularAcceleration;
        private readonly double _dt;

        public AngleToTargetPredictor(Vector2 target, double maxAngularAcceleration, double dt)
        {
            _target = target;
            _maxAngularAcceleration = maxAngularAcceleration;
            _dt = dt;
        }

        public double Evaluate(Pose pose, Velocity v)
        {
            return GetAngleToTarget(MergeSequentialPoseDeltas(pose, PredictPoseDelta(v)));
        }

        public Pose PredictPoseDelta(Velocity v)
        {
            var angularVelocity2 = CalculateAngularVelocityAfterDeceleration(v.Angular);

            return MergeSequentialPoseDeltas(
                ChooseMotionModel(v.Angular).PredictPoseDelta(v, _dt),
                ChooseMotionModel(angularVelocity2).PredictPoseDelta(new Velocity(v.Linear, angularVelocity2), _dt));
        }

        public double GetAngleToTarget(Pose pose)
        {
            return Math.Acos(Vector2.Dot(
                                Vector2.Normalize(_target - pose.Position),
                                new Vector2((float)Math.Cos(pose.Bearing), (float)Math.Sin(pose.Bearing))));
        }

        IMotionModel ChooseMotionModel(double angularVelocity)
        {
            return Math.Abs(angularVelocity) < 1e-5 ? (IMotionModel)new LineMotionModel() : new CircleMotionModel();
        }

        double CalculateAngularVelocityAfterDeceleration(double angularVelocity)
        {
            var neededDeceleration = - angularVelocity/_dt;
            var angularVelocityDecelerated = Math.Abs(neededDeceleration) > _maxAngularAcceleration
                ? angularVelocity - Math.Sign(angularVelocity) * _maxAngularAcceleration*_dt : 0;
            return angularVelocityDecelerated;
        }

        Pose MergeSequentialPoseDeltas(Pose delta1, Pose delta2)
        {
            var originTransform = Matrix.CreateRotationZ((float)delta1.Bearing) * Matrix.CreateTranslation(new Vector3(delta1.Position, 0));
            return new Pose(Vector2.Transform(delta2.Position, originTransform), delta1.Bearing + delta2.Bearing);
        }
    }
}