using System;
using System.Collections.Generic;
using System.Linq;

namespace Brumba.WaiterStupid.McLocalization
{
    public class ParticleFilter<ParticleT, MeasurementT>
    {
        private readonly IByWeightResampler _resampler;

        //particle, control, predicted particle
        public Func<ParticleT, ParticleT, ParticleT> PredictionModel { get; set; }
        //particle, measurement, probability
        public Func<ParticleT, MeasurementT, float> MeasurementInverseModel { get; set; }

        public IEnumerable<ParticleT> Particles { get; private set; }

        public ParticleFilter(IByWeightResampler resampler)
        {
            _resampler = resampler;
        }

        public void Init(IEnumerable<ParticleT> particles)
        {
            Particles = particles.ToList();
        }

        public void Update(ParticleT control, MeasurementT measurement)
        {
            var weightedParticles = Particles.
                Select(p => WeighParticle(measurement, PredictionModel(p, control)));
            Particles = _resampler.Resample(weightedParticles).
                Cast<WeightedParticle<ParticleT>>().Select(ws => ws.Particle).Take(Particles.Count()).ToList();
        }

        private WeightedParticle<ParticleT> WeighParticle(MeasurementT measurement, ParticleT particle)
        {
            return new WeightedParticle<ParticleT>
                {
                    Particle = particle,
                    Weight = MeasurementInverseModel(particle, measurement)
                };
        }
    }

    public class WeightedParticle<ParticleT> : IWeighted
    {
        public ParticleT Particle { get; set; }
        public float Weight { get; set; }
    }
}