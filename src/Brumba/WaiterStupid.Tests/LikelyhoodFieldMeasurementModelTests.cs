using System;
using Brumba.Utils;
using Brumba.WaiterStupid.McLocalization;
using MathNet.Numerics.Distributions;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
    [TestFixture]
    public class LikelyhoodFieldMeasurementModelTests
    {
        private LikelihoodFieldMeasurementModel _lfmm;

        [SetUp]
        public void SetUp()
        {
            _lfmm = new LikelihoodFieldMeasurementModel
            (
                map: new OccupancyGrid(new[,] { { false, true, false }, { false, false, true } }, 1),
                //|   |<R>| O |
                //      V
                //|   | O |   |
                rangefinderProperties: new RangefinderProperties
                {
                    AngularResolution = MathHelper.PiOver2,
                    AngularRange = MathHelper.Pi,
                    MaxRange = 2f
                },
                zeroBeamAngle: -MathHelper.PiOver2,
                sigmaHit: 0.1f,
                weightHit: 0.7f,
                weightRandom: 0.3f
            );
        }

        [Test]
        public void ScanProbability()
        {
            Assert.That(
                _lfmm.ScanProbability(new[] {2f, 1, 1}, new Vector3(1.5f, 1.5f, MathHelper.Pi*3/2)),
                Is.EqualTo(
                    Math.Pow(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(0) + _lfmm.WeightRandom / _lfmm.RangefinderProperties.MaxRange, 2)).Within(1e-5));
        }

        [Test]
        public void BeamProbability()
        {
            Assert.That(_lfmm.BeamProbability(1f, 2, new Vector3(1.5f, 1.5f, MathHelper.Pi * 3 / 2)),
                Is.EqualTo(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(0) + _lfmm.WeightRandom * 1 / _lfmm.RangefinderProperties.MaxRange).Within(1e-5));

            Assert.That(_lfmm.BeamProbability(0.5f, 2, new Vector3(1.5f, 1.5f, MathHelper.Pi * 3 / 2)),
                Is.EqualTo(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(0.5) + _lfmm.WeightRandom * 1 / _lfmm.RangefinderProperties.MaxRange).Within(1e-5));

            Assert.That(_lfmm.BeamProbability(1f, 1, new Vector3(1.5f, 1.5f, MathHelper.Pi * 3 / 2)),
                Is.EqualTo(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(0) + _lfmm.WeightRandom * 1 / _lfmm.RangefinderProperties.MaxRange).Within(1e-5));

            Assert.That(_lfmm.BeamProbability(2f, 0, new Vector3(1.5f, 1.5f, MathHelper.Pi * 3 / 2)),
                Is.EqualTo(1).Within(1e-5));

            Assert.That(_lfmm.BeamProbability(1f, 0, new Vector3(1.5f, 1.5f, MathHelper.Pi * 3 / 2)),
                Is.EqualTo(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(Math.Sqrt(2)) + _lfmm.WeightRandom * 1 / _lfmm.RangefinderProperties.MaxRange).Within(1e-5));
        }

        [Test]
        public void BeamEndPointPosition()
        {
            Assert.That(_lfmm.BeamEndPointPosition(1.5f, 0, new Vector3(1, 1, MathHelper.PiOver2)).EqualsWithin(new Vector2(2.5f, 1), 0.001));
            Assert.That(_lfmm.BeamEndPointPosition(1f, 0, new Vector3(1, 1, MathHelper.PiOver2)).EqualsWithin(new Vector2(2, 1), 0.001));
            Assert.That(_lfmm.BeamEndPointPosition(1.5f, 1, new Vector3(1, 1, MathHelper.PiOver2)).EqualsWithin(new Vector2(1, 2.5f), 0.001));
            Assert.That(_lfmm.BeamEndPointPosition(1.5f, 2, new Vector3(1, 1, MathHelper.PiOver2)).EqualsWithin(new Vector2(-0.5f, 1), 0.001));
        }

        [Test]
        public void RobotToMapTransformation()
        {
            Assert.That(LikelihoodFieldMeasurementModel.RobotToMapTransformation(new Vector2(1, 0), new Vector3(1, 2, 0)).EqualsWithin(new Vector2(2, 2), 0.001));
            Assert.That(LikelihoodFieldMeasurementModel.RobotToMapTransformation(new Vector2(0, 1), new Vector3(1, 2, 0)).EqualsWithin(new Vector2(1, 3), 0.001));

            Assert.That(LikelihoodFieldMeasurementModel.RobotToMapTransformation(new Vector2(1, 0), new Vector3(1, 2, MathHelper.PiOver2)).EqualsWithin(new Vector2(1, 3), 0.001));
            Assert.That(LikelihoodFieldMeasurementModel.RobotToMapTransformation(new Vector2(0, 1), new Vector3(1, 2, MathHelper.PiOver2)).EqualsWithin(new Vector2(0, 2), 0.001));
        }

        [Test]
        public void RangefinderPropertiesBeamToVectorInRobotTransformation()
        {
            var rfp = new RangefinderProperties
            {
                AngularResolution = MathHelper.PiOver2,
                AngularRange = MathHelper.Pi,
                MaxRange = 2f
            };
            Assert.That(rfp.BeamToVectorInRobotTransformation(1, 0, -MathHelper.PiOver2).EqualsWithin(new Vector2(0, -1), 0.001));
            Assert.That(rfp.BeamToVectorInRobotTransformation(1, 1, -MathHelper.PiOver2).EqualsWithin(new Vector2(1, 0), 0.001));
            Assert.That(rfp.BeamToVectorInRobotTransformation(1, 2, -MathHelper.PiOver2).EqualsWithin(new Vector2(0, 1), 0.001));
        }

        [Test]
        public void DistanceToNearestObstacle()
        {
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(1.5f, 0.5f)), Is.EqualTo(0));
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(0.5f, 0.5f)), Is.EqualTo(1).Within(1e-5));
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(1.5f, 1.5f)), Is.EqualTo(1).Within(1e-5));
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(0.5f, 1.5f)), Is.EqualTo(Math.Sqrt(2 * 1 * 1)).Within(1e-5));

            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(0.6f, 0.5f)), Is.EqualTo(0.9).Within(1e-5));
        }
    }
}