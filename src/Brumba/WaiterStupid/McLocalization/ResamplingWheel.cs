using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Brumba.WaiterStupid.McLocalization
{
    [ContractClass(typeof(IWeightedContract))]
    public interface IWeighted
    {
        float Weight { get; set; }
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

    [ContractClass(typeof(IByWeightResamplerContract))]
    public interface IByWeightResampler
    {
        IEnumerable<IWeighted> Resample(IEnumerable<IWeighted> weightedSamples);
    }

    [ContractClassFor(typeof(IByWeightResampler))]
    abstract class IByWeightResamplerContract : IByWeightResampler
    {
        public IEnumerable<IWeighted> Resample(IEnumerable<IWeighted> weightedSamples)
        {
            Contract.Requires(weightedSamples != null);
            Contract.Requires(weightedSamples.ToList().Count() > 1);
            Contract.Ensures(Contract.Result<IEnumerable<IWeighted>>() != null);
            return default(IEnumerable<IWeighted>);
        }
    }

    public class ResamplingWheel : IByWeightResampler
    {
        public IEnumerable<IWeighted> Resample(IEnumerable<IWeighted> weightedSamples)
        {
            var wsList = weightedSamples.ToList();
            var rg = new Random();
            var index = rg.Next(wsList.Count());
            var maxWeight = wsList.Max(ws => ws.Weight);
            var beta = 0.0;

            while (true)
            {
                beta += rg.NextDouble() * 2 * maxWeight;
                while (wsList[index].Weight < beta)
                {
                    beta -= wsList[index].Weight;
                    index = (index + 1) % wsList.Count;
                }
                yield return wsList[index];
            }
        }
    }
}