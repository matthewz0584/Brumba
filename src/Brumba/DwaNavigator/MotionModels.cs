using Brumba.Common;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    [DC.ContractClassAttribute(typeof(IMotionModelContract))]
    public interface IMotionModel
    {
        Pose PredictPoseDelta(double dt);
    }

    [DC.ContractClassForAttribute(typeof(IMotionModel))]
    abstract class IMotionModelContract : IMotionModel
    {
        public Pose PredictPoseDelta(double dt)
        {
            DC.Contract.Requires(dt > 0);
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

        public Pose PredictPoseDelta(double dt)
        {
            return new Pose(
                Center + Vector2.Transform(-Center, Matrix.CreateRotationZ((float)(_v.Angular * dt))),
                _v.Angular * dt);
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

        public Pose PredictPoseDelta(double dt)
        {
            DC.Contract.Ensures(DC.Contract.Result<Pose>().Position.Y == 0 && DC.Contract.Result<Pose>().Bearing == 0);

            return new Pose(new Vector2((float)(_linearVelocity * dt), 0), 0);
        }
    }
}