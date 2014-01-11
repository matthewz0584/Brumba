using System;
using System.Collections.Generic;
using System.Linq;

namespace Brumba.WaiterStupid.McLocalization
{
    public interface IWeighted
    {
        float Weight { get; set; }
    }

    public interface IByWeightResampler
    {
        IEnumerable<IWeighted> Resample(IEnumerable<IWeighted> weightedSamples);
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