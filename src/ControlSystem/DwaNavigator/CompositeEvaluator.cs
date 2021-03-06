using System.Collections.Generic;
using System.Linq;
using Brumba.Common;
using MathNet.Numerics;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DwaNavigator
{
    public class CompositeEvaluator : IVelocityEvaluator
    {
        private IDictionary<IVelocityEvaluator, double> _evaluatorWeights;

        public CompositeEvaluator(IDictionary<IVelocityEvaluator, double> evaluatorWeights)
        {
            DC.Contract.Requires(evaluatorWeights != null);
            DC.Contract.Requires(evaluatorWeights.Values.Sum().AlmostEqualInDecimalPlaces(1, 5));

            EvaluatorWeights = evaluatorWeights;
        }

        public IDictionary<IVelocityEvaluator, double> EvaluatorWeights
        {
            get { return _evaluatorWeights; }
            set
            {
                DC.Contract.Requires(value != null);
                DC.Contract.Requires(value.Values.Sum().AlmostEqualInDecimalPlaces(1, 5));

                _evaluatorWeights = value;
            }
        }

        public double Evaluate(Velocity v)
        {
            DC.Contract.Assert(EvaluatorWeights != null);
            DC.Contract.Assert(EvaluatorWeights.Any());

            return EvaluatorWeights.Select(p => p.Key.Evaluate(v) * p.Value).Sum();
        }
    }
}