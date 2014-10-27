using System.Linq;
using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;

namespace Brumba.DwaNavigator
{
    public interface IVelocityEvaluator
    {
        double Evaluate(Velocity v);
    }

    public class DwaProblemOptimizer
    {
        public DwaProblemOptimizer(IDynamicWindowGenerator dynamicWindowGenerator, IVelocityEvaluator velocityEvaluator, double linearDecelerationMax)
        {
            DynamicWindowGenerator = dynamicWindowGenerator;
            VelocityEvaluator = velocityEvaluator;
            LinearDecelerationMax = linearDecelerationMax;
        }

        public IDynamicWindowGenerator DynamicWindowGenerator { get; private set; }
        public IVelocityEvaluator VelocityEvaluator { get; private set; }
        public double LinearDecelerationMax { get; private set; }

        public Vector2 Optimize(Pose velocity)
        {
            var velocityWheelAccRel = DynamicWindowGenerator.Generate(new Velocity(velocity.Position.Length(), velocity.Bearing));
            return velocityWheelAccRel.
                Where(p => p.Key.Linear >= 0).
                Select(p => new {WheelAccRel = p.Value, Eval = VelocityEvaluator.Evaluate(p.Key)}).
                OrderByDescending(wheelAccRelEval => wheelAccRelEval.Eval).
                First().WheelAccRel;
        }
    }
}