using System;
using System.Linq;
using Brumba.WaiterStupid.McLocalization;
using MathNet.Numerics.Distributions;
//using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.Statistics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
    [TestFixture]
    public class ParticleFilterTests
    {
        [Test]
        public void Acceptance()
        {
            //0-1-2-3-4-5-6-7-8-9-10
            //    I   I     I    
            var mcf = new ParticleFilter<double, bool, double>(
                new ResamplingWheel(),
                PredictionModel<double, double>.FromFunctor((sample, control) => sample + control),
                MeasurementModel<double, bool>.FromFunctor((sample, measurement) =>
                    ((Math.Abs(2 - sample) < 0.1 || Math.Abs(4 - sample) < 0.1 || Math.Abs(7 - sample) < 0.1) &&
                     measurement) ||
                    (!(Math.Abs(2 - sample) < 0.1 || Math.Abs(4 - sample) < 0.1 || Math.Abs(7 - sample) < 0.1) &&
                     !measurement)
                        ? 1.0f : 0.0f
                ));

            mcf.Init(new ContinuousUniform { Lower = 0, Upper = 10 }.Samples().Take(1000));

            mcf.Update(control: 1, measurement: true);

            Assert.That(mcf.Particles.Count(), Is.EqualTo(1000));
            var sh = new Histogram(mcf.Particles, 50, 0.1, 10.1);
            Assert.That(sh[9].Count, Is.GreaterThan(0));
            Assert.That(sh[19].Count, Is.GreaterThan(0));
            Assert.That(sh[34].Count, Is.GreaterThan(0));
            Assert.That(sh[9].Count + sh[19].Count + sh[34].Count, Is.EqualTo(1000));

            mcf.Update(control: 2, measurement: false);

            Assert.That(mcf.Particles.Count(), Is.EqualTo(1000));
            sh = new Histogram(mcf.Particles, 50, 0.1, 10.1);
            Assert.That(sh[29].Count, Is.GreaterThan(0));
            Assert.That(sh[44].Count, Is.GreaterThan(0));
            Assert.That(sh[29].Count + sh[44].Count, Is.EqualTo(1000));

            mcf.Update(control: 1, measurement: true);

            Assert.That(mcf.Particles.Count(), Is.EqualTo(1000));
            sh = new Histogram(mcf.Particles, 50, 0.1, 10.1);
            Assert.That(sh[34].Count, Is.EqualTo(1000));
        }

        [Test]
        public void ResamplingWheel()
        {
            var rw = new ResamplingWheel().Resample(new[]
                    {
                        new WeightedParticle<double> {Particle = 10, Weight = 1},
                        new WeightedParticle<double> {Particle = 9, Weight = 2},
                        new WeightedParticle<double> {Particle = 8, Weight = 3},
                        new WeightedParticle<double> {Particle = 7, Weight = 4},
                        new WeightedParticle<double> {Particle = 6, Weight = 5},
                        new WeightedParticle<double> {Particle = 5, Weight = 6},
                        new WeightedParticle<double> {Particle = 4, Weight = 7},
                        new WeightedParticle<double> {Particle = 3, Weight = 8},
                        new WeightedParticle<double> {Particle = 2, Weight = 9},
                        new WeightedParticle<double> {Particle = 1, Weight = 10},
                    });

            var sh = new Histogram(rw.Cast<WeightedParticle<double>>().Select(ws => ws.Particle).Take(1000), 10, 0, 10);
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

        [Test]
        public void MultivariateParticles()
        {
            //0-0-0
            //0-I-0
            //0-0-0
            var mcf = new ParticleFilter<Vector2, int, Vector2>(new ResamplingWheel(),
                PredictionModel<Vector2, Vector2>.FromFunctor((sample, control) => sample + control),
                MeasurementModel<Vector2, int>.FromFunctor((sample, measurement) =>
                    ((new Vector2(1, 1) - sample).Length() < 0.5 && measurement == 5) ||
                    ((new Vector2(1, 1) - sample).Length() >= 0.5 && measurement != 5)
                        ? 1.0f : 0.0f
                ));

            var u1 = new ContinuousUniform { Lower = 0, Upper = 10 }.Samples().Take(50);
            var u2 = new ContinuousUniform { Lower = 0, Upper = 10 }.Samples().Take(50);
            mcf.Init(from x in u1 from y in u2 select new Vector2((float)x, (float)y));

            mcf.Update(control: new Vector2(1, 1), measurement: 5);

            Assert.That(mcf.Particles.Count(), Is.EqualTo(2500));
            Assert.That(mcf.Particles.Count(s => (new Vector2(1, 1) - s).Length() < 0.5), Is.EqualTo(2500));
        }

        class PredictionModel<TParticle, TControl> : IPredictionModel<TParticle, TControl>
        {
            Func<TParticle, TControl, TParticle> _f;

            public TParticle PredictParticleState(TParticle particle, TControl control)
            {
                return _f(particle, control);
            }

            public static PredictionModel<TParticle, TControl> FromFunctor(
                Func<TParticle, TControl, TParticle> f)
            {
                return new PredictionModel<TParticle, TControl> { _f = f };
            }
        }

        class MeasurementModel<TParticle, TMeasurement> : IMeasurementModel<TParticle, TMeasurement>
        {
            Func<TParticle, TMeasurement, float> _f;

            public float ComputeMeasurementLikelihood(TParticle particle, TMeasurement measurement)
            {
                return _f(particle, measurement);
            }

            public static MeasurementModel<TParticle, TMeasurement> FromFunctor(
                Func<TParticle, TMeasurement, float> f)
            {
                return new MeasurementModel<TParticle, TMeasurement> { _f = f };
            }
        }
    }

    [TestFixture]
    public class McLrfLocalizatorTests
    {
        //[Test]
        //public void Acceptance()
        //{
        //    var mcl = new McLrfLocalizator
        //        {
        //            Map = new Map(),
        //            LrfProperties = new LrfProperties
        //                {
        //                    ZeroBeamAngle = Math.PI / 2,
        //                    AngularResolution = Math.PI / 2,
        //                    MaxRange = 10
        //                },
        //            InitialPose = new Pose()
        //        };

        //    mcl.Update(poseDelta: new Pose(1, 0, 0), measurements: new LrfMeasurement { 1})
        //}
    }

    //[DataContract]
    //public class Velocity : DenseVector
    //{
    //    public Velocity()
    //        : base(3)
    //    {}

    //    public Velocity(DenseVector dv)
    //        : base(dv.Values)
    //    { }

    //    public Velocity(float vx, float vy, float omega)
    //        : base(3)
    //    {
    //        Vx = vx;
    //        Vy = vy;
    //        Omega = omega;
    //    }

    //    [DataMember]
    //    public float Vx
    //    {
    //        get { return this[0]; }
    //        set { this[0] = value; }
    //    }

    //    [DataMember]
    //    public float Vy
    //    {
    //        get { return this[1]; }
    //        set { this[1] = value; }
    //    }

    //    [DataMember]
    //    public float Omega
    //    {
    //        get { return this[2]; }
    //        set { this[2] = value; }
    //    }
    //}
}