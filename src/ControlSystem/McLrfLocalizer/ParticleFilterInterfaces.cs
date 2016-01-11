using System.Collections.Generic;
using DC = System.Diagnostics.Contracts;
using System.Linq;

namespace Brumba.McLrfLocalizer
{
    public interface IPredictionModel<TParticle, in TControl>
    {
        TParticle PredictParticleState(TParticle particle, TControl control);
    }

    [DC.ContractClassAttribute(typeof(IMeasurementModelContract<,>))]
    public interface IMeasurementModel<in TParticle, in TMeasurement>
    {
        float ComputeMeasurementLikelihood(TParticle particle, TMeasurement measurement);
    }

    [DC.ContractClassAttribute(typeof(IWeightedContract))]
    public interface IWeighted
    {
        float Weight { get; set; }
    }

    [DC.ContractClassAttribute(typeof(IByWeightResamplerContract))]
    public interface IByWeightResampler
    {
        IEnumerable<IWeighted> Resample(IEnumerable<IWeighted> weightedSamples);
    }



    [DC.ContractClassForAttribute(typeof(IMeasurementModel<,>))]
    abstract class IMeasurementModelContract<TParticle, TMeasurement> : IMeasurementModel<TParticle, TMeasurement>
    {
        public float ComputeMeasurementLikelihood(TParticle particle, TMeasurement measurement)
        {
            DC.Contract.Ensures(DC.Contract.Result<float>() >= 0);
            return default(float);
        }
    }

    [DC.ContractClassForAttribute(typeof(IWeighted))]
    abstract class IWeightedContract : IWeighted
    {
        public float Weight
        {
            get
            {
                DC.Contract.Ensures(DC.Contract.Result<float>() >= 0);
                return default(float);
            }
            set
            {
                DC.Contract.Requires(value >= 0);
            }
        }
    }

    [DC.ContractClassForAttribute(typeof(IByWeightResampler))]
    abstract class IByWeightResamplerContract : IByWeightResampler
    {
        public IEnumerable<IWeighted> Resample(IEnumerable<IWeighted> weightedSamples)
        {
            DC.Contract.Requires(weightedSamples != null);
            DC.Contract.Requires(weightedSamples.Count() > 1);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<IWeighted>>() != null);
            return default(IEnumerable<IWeighted>);
        }
    }
}