using System.Linq;
using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    [DC.ContractClassAttribute(typeof(IVelocityEvaluatorContract))]
    public interface IVelocityEvaluator
    {
        double Evaluate(Velocity v);
    }

    [DC.ContractClassForAttribute(typeof(IVelocityEvaluator))]
    abstract class IVelocityEvaluatorContract : IVelocityEvaluator
    {
        public double Evaluate(Velocity v)
        {
            DC.Contract.Ensures(DC.Contract.Result<double>() >= 0 && DC.Contract.Result<double>() <= 1);

            return default(double);
        }
    }

    public class DwaProblemOptimizer
    {
        public DwaProblemOptimizer(IDynamicWindowGenerator dynamicWindowGenerator, IVelocityEvaluator velocityEvaluator)
        {
            DynamicWindowGenerator = dynamicWindowGenerator;
            VelocityEvaluator = velocityEvaluator;
        }

        public IDynamicWindowGenerator DynamicWindowGenerator { get; private set; }
        public IVelocityEvaluator VelocityEvaluator { get; private set; }

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