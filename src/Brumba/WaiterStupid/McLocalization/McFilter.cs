using System;
using System.Collections.Generic;
using System.Linq;

namespace Brumba.WaiterStupid.McLocalization
{
    public class McFilter
    {
        private readonly IResampler _resampler;

        //sample, control, predicted sample
        public Func<double, double, double> PredictionModel { get; set; }
        //sample, measurement, probability
        public Func<double, bool, float> MeasurementInverseModel { get; set; }

        public IEnumerable<double> Samples { get; private set; }

        public McFilter(IResampler resampler)
        {
            _resampler = resampler;
        }

        public void Init(IEnumerable<double> samples)
        {
            Samples = samples.ToList();
        }

        public void Update(double control, bool measurement)
        {
            var weightedSamples = Samples.
                Select(s => PredictionModel(s, control)).
                Select(ps => new WeightedSample
                    {
                        Sample = ps,
                        Weight = MeasurementInverseModel(ps, measurement)
                    });
            Samples = _resampler.Resample(weightedSamples).Take(Samples.Count());
        }
    }

    public class WeightedSample
    {
        public double Sample { get; set; }
        public float Weight { get; set; }
    }
}