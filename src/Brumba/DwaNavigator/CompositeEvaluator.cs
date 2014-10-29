using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class CompositeEvaluator : IVelocityEvaluator
    {
        public IDictionary<IVelocityEvaluator, double> EvaluatorWeights { get; set; }

        public CompositeEvaluator(IDictionary<IVelocityEvaluator, double> evaluatorWeights)
        {
            DC.Contract.Requires(evaluatorWeights != null);
            DC.Contract.Requires(evaluatorWeights.Values.Sum().AlmostEqualInDecimalPlaces(1, 5));

            EvaluatorWeights = evaluatorWeights;
        }

        public double Evaluate(Velocity v)
        {
            return EvaluatorWeights.Select(p => p.Key.Evaluate(v) * p.Value).Sum();
        }
    }
}