using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Brumba.WaiterStupid.McLocalization
{
    public class ParticleFilter<TParticle, TMeasurement, TControl>
    {
        readonly IByWeightResampler _resampler;

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(PredictionModel != null);
            Contract.Invariant(MeasurementModel != null);
        }

        public IPredictionModel<TParticle, TControl> PredictionModel { get; private set; }

        public IMeasurementModel<TParticle, TMeasurement> MeasurementModel { get; private set; }

        public IEnumerable<TParticle> Particles { get; private set; }

        public ParticleFilter(IByWeightResampler resampler, IPredictionModel<TParticle, TControl> predictionModel, IMeasurementModel<TParticle, TMeasurement> measurementModel)
        {
            Contract.Requires(resampler != null);
            Contract.Requires(predictionModel != null);
            Contract.Requires(measurementModel != null);

            _resampler = resampler;
            PredictionModel = predictionModel;
            MeasurementModel = measurementModel;
        }

        public void Init(IEnumerable<TParticle> particles)
        {
            Contract.Requires(particles != null);
            Contract.Requires(particles.ToList().Count() > 1);
            Contract.Ensures(Particles != null);
            Contract.Ensures(Particles.Count() > 1);

            Particles = particles.ToList();
        }

        public void Update(TControl control, TMeasurement measurement)
        {
            Contract.Requires(Particles != null);
            Contract.Requires(Particles.Count() > 1);
            Contract.Requires(PredictionModel != null);
            Contract.Requires(MeasurementModel != null);
            Contract.Ensures(Contract.OldValue(Particles.Count()) == Particles.Count());

            var weightedParticles = Particles.
                Select(p => WeighParticle(measurement, PredictionModel.PredictParticleState(p, control))).ToList();

            Contract.Assume(Contract.Exists(weightedParticles, wp => wp.Weight > 0));

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
                    Weight = MeasurementModel.ComputeMeasurementLikelihood(particle, measurement)
                };
        }
    }

    public class WeightedParticle<TParticle> : IWeighted
    {
        public TParticle Particle { get; set; }
        public float Weight { get; set; }
    }
}