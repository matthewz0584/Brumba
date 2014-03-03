using System;
using System.Collections.Generic;
using System.Linq;

namespace Brumba.WaiterStupid.McLocalization
{
    public class ParticleFilter<TParticle, TMeasurement>
    {
        private readonly IByWeightResampler _resampler;

        //particle, control, predicted particle
        public Func<TParticle, TParticle, TParticle> PredictionModel { get; set; }
        //particle, measurement, probability
        public Func<TParticle, TMeasurement, float> MeasurementModel { get; set; }

        public IEnumerable<TParticle> Particles { get; private set; }

        public ParticleFilter(IByWeightResampler resampler)
        {
            _resampler = resampler;
        }

        public void Init(IEnumerable<TParticle> particles)
        {
            Particles = particles.ToList();
        }

        public void Update(TParticle control, TMeasurement measurement)
        {
            var weightedParticles = Particles.
                Select(p => WeighParticle(measurement, PredictionModel(p, control)));
            Particles = _resampler.Resample(weightedParticles).
                Cast<WeightedParticle<TParticle>>().Select(ws => ws.Particle).Take(Particles.Count()).ToList();
        }

        private WeightedParticle<TParticle> WeighParticle(TMeasurement measurement, TParticle particle)
        {
            return new WeightedParticle<TParticle>
                {
                    Particle = particle,
                    Weight = MeasurementModel(particle, measurement)
                };
        }
    }

    public class WeightedParticle<TParticle> : IWeighted
    {
        public TParticle Particle { get; set; }
        public float Weight { get; set; }
    }
}