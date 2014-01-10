using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.WaiterStupid.McLocalization;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.Statistics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
    [TestFixture]
    public class McFilterTests
    {
        [Test]
        public void Acceptance()
        {
            //0-1-2-3-4-5-6-7-8-9-10
            //    I   I     I    
            var mcf = new McFilter(new ResamplingWheel())
                {
                    PredictionModel = (sample, control) => sample + control,
                    MeasurementInverseModel =
                        (sample, measurement) =>
                        ((Math.Abs(2 - sample) < 0.1 || Math.Abs(4 - sample) < 0.1 || Math.Abs(7 - sample) < 0.1) && measurement) ||
                        (!(Math.Abs(2 - sample) < 0.1 || Math.Abs(4 - sample) < 0.1 || Math.Abs(7 - sample) < 0.1) && !measurement)
                            ? 1.0f : 0.0f,
                };

            mcf.Init(new ContinuousUniform { Lower = 0, Upper = 10 }.Samples().Take(1000));

            mcf.Update(control: 1, measurement: true);

            Assert.That(mcf.Samples.Count(), Is.EqualTo(1000));
            var sh = new Histogram(mcf.Samples, 50, 0.1, 10.1);
            Assert.That(sh[9].Count, Is.GreaterThan(0));
            Assert.That(sh[19].Count, Is.GreaterThan(0));
            Assert.That(sh[34].Count, Is.GreaterThan(0));
            Assert.That(sh[9].Count + sh[19].Count + sh[34].Count, Is.EqualTo(1000));

            mcf.Update(control: 2, measurement: false);

            Assert.That(mcf.Samples.Count(), Is.EqualTo(1000));
            sh = new Histogram(mcf.Samples, 50, 0.1, 10.1);
            Assert.That(sh[29].Count, Is.GreaterThan(0));
            Assert.That(sh[44].Count, Is.GreaterThan(0));
            Assert.That(sh[29].Count + sh[44].Count, Is.EqualTo(1000));

            mcf.Update(control: 1, measurement: true);

            Assert.That(mcf.Samples.Count(), Is.EqualTo(1000));
            sh = new Histogram(mcf.Samples, 50, 0.1, 10.1);
            Assert.That(sh[34].Count, Is.EqualTo(1000));
        }

        [Test]
        public void ResamplingWheel()
        {
            var rw = new ResamplingWheel().Resample(new[]
                    {
                        new WeightedSample {Sample = 10, Weight = 1},
                        new WeightedSample {Sample = 9, Weight = 2},
                        new WeightedSample {Sample = 8, Weight = 3},
                        new WeightedSample {Sample = 7, Weight = 4},
                        new WeightedSample {Sample = 6, Weight = 5},
                        new WeightedSample {Sample = 5, Weight = 6},
                        new WeightedSample {Sample = 4, Weight = 7},
                        new WeightedSample {Sample = 3, Weight = 8},
                        new WeightedSample {Sample = 2, Weight = 9},
                        new WeightedSample {Sample = 1, Weight = 10},
                    });

            var sh = new Histogram(rw.Take(1000), 10, 0, 10);
            Assert.That(sh[0].Count, Is.EqualTo(10.0 / 55 * 1000).Within(25));
            Assert.That(sh[1].Count, Is.EqualTo(9.0 / 55 * 1000).Within(25));
            Assert.That(sh[2].Count, Is.EqualTo(8.0 / 55 * 1000).Within(25));
            Assert.That(sh[3].Count, Is.EqualTo(7.0 / 55 * 1000).Within(25));
            Assert.That(sh[4].Count, Is.EqualTo(6.0 / 55 * 1000).Within(25));
            Assert.That(sh[5].Count, Is.EqualTo(5.0 / 55 * 1000).Within(25));
            Assert.That(sh[6].Count, Is.EqualTo(4.0 / 55 * 1000).Within(25));
            Assert.That(sh[7].Count, Is.EqualTo(3.0 / 55 * 1000).Within(25));
            Assert.That(sh[8].Count, Is.EqualTo(2.0 / 55 * 1000).Within(25));
            Assert.That(sh[9].Count, Is.EqualTo(1.0 / 55 * 1000).Within(25));
        }

        //[Test]
        //public void MultivariateSamples()
        //{
        //    //0-0-0
        //    //0-I-0
        //    //0-0-0
        //    var mcf = new McFilter(new ResamplingWheel())
        //    {
        //        PredictionModel = (sample, control) => sample + control,
        //        MeasurementInverseModel =
        //            (sample, measurement) =>
        //            ((new Vector2(1, 1) - sample).Length() < 0.1 && measurement == 5) ||
        //            ((new Vector2(1, 1) - sample).Length() >= 0.1 && measurement != 5)
        //                ? 1.0f : 0.0f,
        //    };

        //    var u1 = new ContinuousUniform {Lower = 0, Upper = 10}.Samples().Take(50);
        //    var u2 = new ContinuousUniform {Lower = 0, Upper = 10}.Samples().Take(50);
        //    mcf.Init(from x in u1 from y in u2 select new Vector2((float)x, (float)y));

        //    mcf.Update(control: new Vector2(1, 1), measurement: 5);

        //    Assert.That(mcf.Samples.Count(), Is.EqualTo(2500));
        //    Assert.That(mcf.Samples.Where(s => (new Vector2(1, 1) - s).Length() < 0.1).Count, Is.EqualTo(2500));
        //}
    }
}