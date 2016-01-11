using Brumba.Common;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DiffDriveOdometry
{
    [DC.ContractClassAttribute(typeof(IMotionModelContract))]
    public interface IMotionModel
    {
        Pose PredictPoseDeltaAsForVelocity(double dt);

        Pose PredictPoseDeltaAsForDistance();
    }

    [DC.ContractClassForAttribute(typeof(IMotionModel))]
    abstract class IMotionModelContract : IMotionModel
    {
        public Pose PredictPoseDeltaAsForVelocity(double dt)
        {
            DC.Contract.Requires(dt > 0);
            return default(Pose);
        }

        public Pose PredictPoseDeltaAsForDistance()
        {
            return default(Pose);
        }
    }

    public class CirclularMotionModel : IMotionModel
    {
        private readonly Velocity _v;
        private readonly double _radius;
        private readonly Vector2 _center;

        public CirclularMotionModel(Velocity v)
        {
            DC.Contract.Requires(v.Angular != 0);
            DC.Contract.Ensures(Radius > double.MinValue && Radius < double.MaxValue);
            DC.Contract.Ensures(Center.Y == (float)Radius);

            _v = v;
            _radius = v.Linear / v.Angular;
            _center = new Vector2(0, (float)_radius);
        }

        public Pose PredictPoseDeltaAsForVelocity(double dt)
        {
            return PredictPoseDelta(_v.Angular * dt);
        }

        public Pose PredictPoseDeltaAsForDistance()
        {
            return PredictPoseDelta(_v.Angular);
        }

        Pose PredictPoseDelta(double angle)
        {
            return new Pose(Center + Vector2.Transform(-Center, Matrix.CreateRotationZ((float)angle)), angle);
        }

        public Vector2 Center
        {
            get { return _center; }
        }

        public double Radius
        {
            get { return _radius; }
        }
    }

    public class LinearMotionModel : IMotionModel
    {
        private readonly double _linearVelocity;

        public LinearMotionModel(double linearVelocity)
        {
            _linearVelocity = linearVelocity;
        }

        public Pose PredictPoseDeltaAsForVelocity(double dt)
        {
            DC.Contract.Ensures(DC.Contract.Result<Pose>().Position.Y == 0 && DC.Contract.Result<Pose>().Bearing == 0);

            return PredictPoseDelta((_linearVelocity * dt));
        }

        public Pose PredictPoseDeltaAsForDistance()
        {
            DC.Contract.Ensures(DC.Contract.Result<Pose>().Position.Y == 0 && DC.Contract.Result<Pose>().Bearing == 0);

            return PredictPoseDelta(_linearVelocity);
        }

        Pose PredictPoseDelta(double dist)
        {
            return new Pose(new Vector2((float)dist, 0), 0);
        }
    }
}