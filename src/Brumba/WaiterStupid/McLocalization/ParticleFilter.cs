using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            Contract.Requires(resampler != null);

            _resampler = resampler;
        }

        public void Init(IEnumerable<TParticle> particles)
        {
            Contract.Requires(particles != null);
            Contract.Requires(particles.Count() > 1);
            Contract.Ensures(Particles != null);
            Contract.Ensures(Particles.Count() > 1);

            Particles = particles.ToList();
        }

        public void Update(TParticle control, TMeasurement measurement)
        {
            Contract.Requires(Particles != null);
            Contract.Requires(Particles.Count() > 1);
            Contract.Requires(PredictionModel != null);
            Contract.Requires(MeasurementModel != null);
            Contract.Ensures(Contract.OldValue(Particles.Count()) == Particles.Count());

            var weightedParticles = Particles.
                Select(p => WeighParticle(measurement, PredictionModel(p, control)));
            Particles = _resampler.Resample(weightedParticles).
                Cast<WeightedParticle<TParticle>>().Select(ws => ws.Particle).Take(Particles.Count()).ToList();
        }

        WeightedParticle<TParticle> WeighParticle(TMeasurement measurement, TParticle particle)
        {
            Contract.Ensures(Contract.Result<WeightedParticle<TParticle>>().Particle.Equals(particle));
            Contract.Ensures(Contract.Result<WeightedParticle<TParticle>>().Weight >= 0);

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