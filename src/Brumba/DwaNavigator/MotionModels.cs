using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public interface IMotionModel
    {
        Pose PredictPoseDelta(Velocity v, double dt);
    }

    public class CircleMotionModel : IMotionModel
    {
        public Pose PredictPoseDelta(Velocity v, double dt)
        {
            DC.Contract.Requires(v.Angular != 0);

            return new Pose(
                GetCenter(v) + Vector2.Transform(-GetCenter(v), Matrix.CreateRotationZ((float)(v.Angular * dt))),
                v.Angular * dt);
        }

        public Vector2 GetCenter(Velocity v)
        {
            return new Vector2(0, (float)GetRadius(v));
        }

        public double GetRadius(Velocity v)
        {
            return v.Linear / v.Angular;
        }
    }

    public class LineMotionModel : IMotionModel
    {
        public Pose PredictPoseDelta(double linearVelocity, double dt)
        {
            return new Pose(new Vector2((float)(linearVelocity * dt), 0), 0);
        }

        public Pose PredictPoseDelta(Velocity v, double dt)
        {
            return PredictPoseDelta(v.Linear, dt);
        }
    }
}