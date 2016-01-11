using System.Collections.Generic;
using DC = System.Diagnostics.Contracts;
using System.Linq;

namespace Brumba.McLrfLocalizer
{
    public class ParticleFilter<TParticle, TMeasurement, TControl>
    {
        readonly IByWeightResampler _resampler;

        [DC.ContractInvariantMethod]
        void ObjectInvariant()
        {
            DC.Contract.Invariant(PredictionModel != null);
            DC.Contract.Invariant(MeasurementModel != null);
        }

        public IPredictionModel<TParticle, TControl> PredictionModel { get; private set; }

        public IMeasurementModel<TParticle, TMeasurement> MeasurementModel { get; private set; }

        public IEnumerable<TParticle> Particles { get; private set; }

        public ParticleFilter(IByWeightResampler resampler, IPredictionModel<TParticle, TControl> predictionModel, IMeasurementModel<TParticle, TMeasurement> measurementModel)
        {
            DC.Contract.Requires(resampler != null);
            DC.Contract.Requires(predictionModel != null);
            DC.Contract.Requires(measurementModel != null);

            _resampler = resampler;
            PredictionModel = predictionModel;
            MeasurementModel = measurementModel;
        }

        public void Init(IEnumerable<TParticle> particles)
        {
            DC.Contract.Requires(particles != null);
            DC.Contract.Requires(particles.ToList().Count() > 1);
            DC.Contract.Ensures(Particles != null);
            DC.Contract.Ensures(Particles.Count() > 1);

            Particles = particles.ToList();
        }

        public void Update(TControl control, TMeasurement measurement)
        {
            DC.Contract.Requires(Particles != null);
            DC.Contract.Requires(Particles.Count() > 1);
            DC.Contract.Requires(PredictionModel != null);
            DC.Contract.Requires(MeasurementModel != null);
            DC.Contract.Ensures(DC.Contract.OldValue(Particles.Count()) == Particles.Count());

			var weightedParticles = Particles.//AsParallel().
				Select(p => WeighParticle(measurement, PredictionModel.PredictParticleState(p, control))).ToList();

            DC.Contract.Assume(DC.Contract.Exists(weightedParticles, wp => wp.Weight > 0));

            Particles = _resampler.Resample(weightedParticles).
                Cast<WeightedParticle<TParticle>>().Select(ws => ws.Particle).Take(Particles.Count()).ToList();
        }

        WeightedParticle<TParticle> WeighParticle(TMeasurement measurement, TParticle particle)
        {
            DC.Contract.Ensures(DC.Contract.Result<WeightedParticle<TParticle>>().Particle.Equals(particle));
            DC.Contract.Ensures(DC.Contract.Result<WeightedParticle<TParticle>>().Weight >= 0);

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