using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.McLrfLocalizer.Tests
{
    [TestFixture]
    public class OdometryMotionModelTests
    {
        [Test]
        public void PredictParticleState()
        {
            var omm = new OdometryMotionModel(
                map: new OccupancyGrid(new[,] {{false, true, false}, {false, false, true}}, 1),
                rotNoiseCoeffs: new Vector2(0.01f, 0.01f),
                transNoiseCoeffs: new Vector2(0.01f, 0.01f));

            var cleanPrediction = new Pose(new Vector2(1.9f, 1.1f) + new Vector2(-0.5f, 0.5f), 0 + MathHelper.PiOver4);

            var prediction1 = omm.PredictParticleState(new Pose(new Vector2(1.9f, 1.1f), 0), new Pose(new Vector2(-0.5f, 0.5f), Constants.PiOver4));
            Console.WriteLine(prediction1);
            Assert.That(prediction1.Position.EqualsRelatively(cleanPrediction.Position, 0.05));
            Assert.That(prediction1.Bearing, Is.EqualTo(cleanPrediction.Bearing).Within(0.1));

            var prediction2 = omm.PredictParticleState(new Pose(new Vector2(1.9f, 1.1f), 0), new Pose(new Vector2(-0.5f, 0.5f), Constants.PiOver4));
            Console.WriteLine(prediction2);
            Assert.That(prediction2.Position.EqualsRelatively(cleanPrediction.Position, 0.05));
            Assert.That(prediction2.Bearing, Is.EqualTo(cleanPrediction.Bearing).Within(0.1));

            Assert.That(prediction1, Is.Not.EqualTo(prediction2));
        }

        [Test]
        public void PredictParticleStateThetaBetweenZeroAndTwoPi()
        {
            var omm = new OdometryMotionModel(
                map: new OccupancyGrid(new[,] { { false, true, false }, { false, false, true } }, 1),
                rotNoiseCoeffs: new Vector2(0.01f, 0.01f),
                transNoiseCoeffs: new Vector2(0.01f, 0.01f));

            Assert.That(omm.PredictParticleState(new Pose(new Vector2(0, 0), Constants.Pi2 - Constants.PiOver4), new Pose(new Vector2(0, 0), Constants.PiOver2)).Bearing,
                Is.EqualTo(Constants.PiOver4).Within(1e-1));

            Assert.That(omm.PredictParticleState(new Pose(new Vector2(0, 0), Constants.Pi - Constants.PiOver4), new Pose(new Vector2(0, 0), Constants.PiOver2)).Bearing,
                Is.EqualTo(5 * Constants.PiOver4).Within(1e-1));
        }

        [Test]
        public void OdometryToRotTransRotSequence()
        {
            Assert.That(OdometryMotionModel.OdometryToRotTransRotSequence(new Pose(new Vector2(1, 2), 0), new Pose(new Vector2(1, 0), 0)),
                Is.EqualTo(new Vector3(0, 1, 0)));

            Assert.That(OdometryMotionModel.OdometryToRotTransRotSequence(new Pose(new Vector2(1, 2), 0), new Pose(new Vector2(1, 0), MathHelper.PiOver4)),
                Is.EqualTo(new Vector3(0, 1, MathHelper.PiOver4)));

            Assert.That(OdometryMotionModel.OdometryToRotTransRotSequence(new Pose(new Vector2(1, 2), Constants.PiOver4), new Pose(new Vector2(1, 0), 7 * Constants.PiOver4)).
				EqualsRelatively(new Vector3(-MathHelper.PiOver4, 1, 0), 1e-5));

            Assert.That(OdometryMotionModel.OdometryToRotTransRotSequence(new Pose(new Vector2(1, 2), Constants.PiOver4), new Pose(new Vector2(1, 0), Constants.PiOver4)).
                EqualsRelatively(new Vector3(-MathHelper.PiOver4, 1, MathHelper.PiOver2), 1e-5));
        }

        [Test]
        public void RotTransRotSequenceToOdometry()
        {
            Assert.That(OdometryMotionModel.RotTransRotSequenceToOdometry(new Pose(new Vector2(1, 2), 0), new Vector3(MathHelper.PiOver4, 0, 0)),
                Is.EqualTo(new Pose(new Vector2(0, 0), MathHelper.PiOver4)));

            Assert.That(OdometryMotionModel.RotTransRotSequenceToOdometry(new Pose(new Vector2(1, 2), 0), new Vector3(MathHelper.PiOver4, 1, 0)),
                Is.EqualTo(new Pose(new Vector2(1 / (float)Constants.Sqrt2, 1 / (float)Constants.Sqrt2), MathHelper.PiOver4)));

            Assert.That(OdometryMotionModel.RotTransRotSequenceToOdometry(new Pose(new Vector2(1, 2), 0), new Vector3(MathHelper.PiOver4, 1, -MathHelper.PiOver4)),
                Is.EqualTo(new Pose(new Vector2(1 / (float)Constants.Sqrt2, 1 / (float)Constants.Sqrt2), 0)));

            var o1 = OdometryMotionModel.RotTransRotSequenceToOdometry(new Pose(new Vector2(0, 0), Constants.Pi), new Vector3(MathHelper.Pi, 1, MathHelper.Pi));
            Assert.That(o1.Position.EqualsRelatively(new Vector2(1, 0), 1e-5));
            Assert.That(o1.Bearing, Is.EqualTo(0));

            var o2 = OdometryMotionModel.RotTransRotSequenceToOdometry(new Pose(new Vector2(0, 0), Constants.Pi), new Vector3(MathHelper.PiOver2, 1, MathHelper.Pi));
            Assert.That(o2.Position.EqualsRelatively(new Vector2(0, -1), 1e-5));
            Assert.That(o2.Bearing, Is.EqualTo(3 * MathHelper.PiOver2));
        }

        [Test]
        public void ComputeRotTransRotNoise()
        {
            var omm = new OdometryMotionModel(
                map: new OccupancyGrid(new[,] { { false }, { false } }, 1),
                rotNoiseCoeffs: new Vector2(1, 1),
                transNoiseCoeffs: new Vector2(1, 1));

            var noiseBase = Enumerable.Range(0, 100).Select(i => omm.ComputeRotTransRotNoise(new Vector3(0.1f, 0.1f, 0.1f)));
            var noiseBaseRot1StdDev = StdDev(noiseBase, v => v.X);
            var noiseBaseTransStdDev = StdDev(noiseBase, v => v.Y);
            var noiseBaseRot2StdDev = StdDev(noiseBase, v => v.Z);

            var noiseRot1 = Enumerable.Range(0, 100).Select(i => omm.ComputeRotTransRotNoise(new Vector3(1, 0.1f, 0.1f)));
            Assert.That(StdDev(noiseRot1, v => v.X), Is.GreaterThan(noiseBaseRot1StdDev * 2));
            Assert.That(StdDev(noiseRot1, v => v.Y), Is.GreaterThan(noiseBaseTransStdDev * 2));
            Assert.That(StdDev(noiseRot1, v => v.Z), Is.Not.GreaterThan(noiseBaseRot2StdDev * 2));

            var noiseTrans = Enumerable.Range(0, 100).Select(i => omm.ComputeRotTransRotNoise(new Vector3(0.1f, 1, 0.1f)));
            Assert.That(StdDev(noiseTrans, v => v.X), Is.GreaterThan(noiseBaseRot1StdDev * 2));
            Assert.That(StdDev(noiseTrans, v => v.Y), Is.GreaterThan(noiseBaseTransStdDev * 2));
            Assert.That(StdDev(noiseTrans, v => v.Z), Is.GreaterThan(noiseBaseRot2StdDev * 2));

            var noiseRot2 = Enumerable.Range(0, 100).Select(i => omm.ComputeRotTransRotNoise(new Vector3(0.1f, 0.1f, 1)));
            Assert.That(StdDev(noiseRot2, v => v.X), Is.Not.GreaterThan(noiseBaseRot1StdDev * 2));
            Assert.That(StdDev(noiseRot2, v => v.Y), Is.GreaterThan(noiseBaseTransStdDev * 2));
            Assert.That(StdDev(noiseRot2, v => v.Z), Is.GreaterThan(noiseBaseRot2StdDev * 2));
        }

        [Test]
        public void PredictParticleStateContradictionWithMap()
        {
            var omm = new OdometryMotionModel(
                map: new OccupancyGrid(new[,] { { false, true, false }, { false, false, true } }, 1),
                rotNoiseCoeffs: new Vector2(0.1f, 0.1f),
                transNoiseCoeffs: new Vector2(0.1f, 0.1f));

            Assert.That(omm.PredictParticleState(new Pose(new Vector2(1.9f, 1.5f), 0), new Pose(new Vector2(0.6f, 0), 0)),
                Is.EqualTo(new Pose(new Vector2(1.9f, 1.5f), 0)));//move to center of occupied cell, no chance to deviate

            Assert.That(omm.PredictParticleState(new Pose(new Vector2(0, 0), 0), new Pose(new Vector2(2.1f, 1.1f), 0)),
                Is.Not.EqualTo(new Pose(new Vector2(0, 0), 0)));//move to occupied cell, near its border
        }

        [Test]
        public void PredictParticleStateOutsideOfMap()
        {
            var omm = new OdometryMotionModel(
                map: new OccupancyGrid(new[,] { { false, true, false }, { false, false, true } }, 1),
                rotNoiseCoeffs: new Vector2(0.01f, 0.01f),
                transNoiseCoeffs: new Vector2(0.01f, 0.01f));

            var prediction = omm.PredictParticleState(new Pose(new Vector2(2.9f, 0.5f), 0), new Pose(new Vector2(0.2f, 0), 0));
            Assert.That(prediction.Position.EqualsRelatively(new Vector2(3.1f, 0.5f), 0.05));//move outside of the map
            Assert.That(prediction.Bearing.ToMinAbsValueAngle(), Is.EqualTo(0).Within(0.01));//move outside of the map
        }

        static double StdDev(IEnumerable<Vector3> vecs, Func<Vector3, float> selector)
        {
            return new DescriptiveStatistics(vecs.Select(rtr => (double)selector(rtr))).StandardDeviation;
        }
    }
}