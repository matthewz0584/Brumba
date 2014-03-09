using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Brumba.WaiterStupid.McLocalization
{
    public interface IPredictionModel<TParticle, in TControl>
    {
        TParticle PredictParticleState(TParticle particle, TControl control);
    }

    [ContractClass(typeof(IMeasurementModelContract<,>))]
    public interface IMeasurementModel<in TParticle, in TMeasurement>
    {
        float ComputeMeasurementLikelihood(TParticle particle, TMeasurement measurement);
    }

    [ContractClass(typeof(IWeightedContract))]
    public interface IWeighted
    {
        float Weight { get; set; }
    }

    [ContractClass(typeof(IByWeightResamplerContract))]
    public interface IByWeightResampler
    {
        IEnumerable<IWeighted> Resample(IEnumerable<IWeighted> weightedSamples);
    }



    [ContractClassFor(typeof(IMeasurementModel<,>))]
    abstract class IMeasurementModelContract<TParticle, TMeasurement> : IMeasurementModel<TParticle, TMeasurement>
    {
        public float ComputeMeasurementLikelihood(TParticle particle, TMeasurement measurement)
        {
            Contract.Ensures(Contract.Result<float>() >= 0);
            return default(float);
        }
    }

    [ContractClassFor(typeof(IWeighted))]
    abstract class IWeightedContract : IWeighted
    {
        public float Weight
        {
            get
            {
                Contract.Ensures(Contract.Result<float>() >= 0);
                return default(float);
            }
            set
            {
                Contract.Requires(value >= 0);
            }
        }
    }

    [ContractClassFor(typeof(IByWeightResampler))]
    abstract class IByWeightResamplerContract : IByWeightResampler
    {
        public IEnumerable<IWeighted> Resample(IEnumerable<IWeighted> weightedSamples)
        {
            Contract.Requires(weightedSamples != null);
            Contract.Requires(weightedSamples.Count() > 1);
            Contract.Ensures(Contract.Result<IEnumerable<IWeighted>>() != null);
            return default(IEnumerable<IWeighted>);
        }
    }
}