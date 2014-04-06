using System;
using Brumba.MapProvider;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.McLrfLocalizer.Tests
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
                    AngularResolution = Constants.PiOver2,
                    AngularRange = Constants.Pi,
                    MaxRange = 2f,
                    OriginPose = new Pose(new Vector2(), 0)
                },
                sigmaHit: 0.1f,
                weightHit: 0.7f,
                weightRandom: 0.3f
            );
        }

        [Test]
        public void ScanLikelihood()
        {
            Assert.That(
                _lfmm.ComputeMeasurementLikelihood(new Pose(new Vector2(1.5f, 1.5f), 3 * Constants.PiOver2), new[] { 2f, 1, 1 }),
                Is.EqualTo(
                    2 *(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(0) + _lfmm.WeightRandom / _lfmm.RangefinderProperties.MaxRange) + 0.1).Within(1e-5));
        }

        [Test]
        public void BeamLikelihood()
        {
            Assert.That(_lfmm.BeamLikelihood(new Pose(new Vector2(1.5f, 1.5f), 3 * Constants.PiOver2), 1f, 2),
                Is.EqualTo(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(0) + _lfmm.WeightRandom * 1 / _lfmm.RangefinderProperties.MaxRange).Within(1e-5));

            Assert.That(_lfmm.BeamLikelihood(new Pose(new Vector2(1.5f, 1.5f), 3 * Constants.PiOver2), 0.4f, 2),
                Is.EqualTo(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(0) + _lfmm.WeightRandom * 1 / _lfmm.RangefinderProperties.MaxRange).Within(1e-5));

            Assert.That(_lfmm.BeamLikelihood(new Pose(new Vector2(1.5f, 1.5f), 3 * Constants.PiOver2), 0.2f, 2),
                Is.EqualTo(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(0.2) + _lfmm.WeightRandom * 1 / _lfmm.RangefinderProperties.MaxRange).Within(1e-5));

            Assert.That(_lfmm.BeamLikelihood(new Pose(new Vector2(1.5f, 1.5f), 3 * Constants.PiOver2), 1f, 1),
                Is.EqualTo(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(0) + _lfmm.WeightRandom * 1 / _lfmm.RangefinderProperties.MaxRange).Within(1e-5));

            Assert.That(_lfmm.BeamLikelihood(new Pose(new Vector2(1.5f, 1.5f), 3 * Constants.PiOver2), 1f, 0),
                Is.EqualTo(_lfmm.WeightHit * new Normal(0, _lfmm.SigmaHit).Density(Constants.Sqrt2 - 0.6) + _lfmm.WeightRandom * 1 / _lfmm.RangefinderProperties.MaxRange).Within(1e-5));
        }

        [Test]
        public void BeamOutOfMap()
        {
            //Если робот вылез за карту - проблема высшего уровня, фильтр не должен быть вызван с такой одометрией
            Assert.That(_lfmm.BeamLikelihood(new Pose(new Vector2(1.5f, 1.5f), 0), 1f, 2), Is.EqualTo(0));
        }

        [Test]
        public void BeamEndPointPosition()
        {
            Assert.That(_lfmm.BeamEndPointPosition(new Pose(new Vector2(1, 1), Constants.PiOver2), 1.5f, 0).EqualsRelatively(new Vector2(2.5f, 1), 0.001));
            Assert.That(_lfmm.BeamEndPointPosition(new Pose(new Vector2(1, 1), Constants.PiOver2), 1f, 0).EqualsRelatively(new Vector2(2, 1), 0.001));
            Assert.That(_lfmm.BeamEndPointPosition(new Pose(new Vector2(1, 1), Constants.PiOver2), 1.5f, 1).EqualsRelatively(new Vector2(1, 2.5f), 0.001));
            Assert.That(_lfmm.BeamEndPointPosition(new Pose(new Vector2(1, 1), Constants.PiOver2), 1.5f, 2).EqualsRelatively(new Vector2(-0.5f, 1), 0.001));
        }

        [Test]
        public void RobotToMapTransformation()
        {
            Assert.That(LikelihoodFieldMeasurementModel.RobotToMapTransformation(new Vector2(1, 0), new Pose(new Vector2(1, 2), 0)).EqualsRelatively(new Vector2(2, 2), 0.001));
            Assert.That(LikelihoodFieldMeasurementModel.RobotToMapTransformation(new Vector2(0, 1), new Pose(new Vector2(1, 2), 0)).EqualsRelatively(new Vector2(1, 3), 0.001));

            Assert.That(LikelihoodFieldMeasurementModel.RobotToMapTransformation(new Vector2(1, 0), new Pose(new Vector2(1, 2), Constants.PiOver2)).EqualsRelatively(new Vector2(1, 3), 0.001));
            Assert.That(LikelihoodFieldMeasurementModel.RobotToMapTransformation(new Vector2(0, 1), new Pose(new Vector2(1, 2), Constants.PiOver2)).EqualsRelatively(new Vector2(0, 2), 0.001));
        }

        [Test]
        public void RangefinderPropertiesBeamToVectorInRobotTransformation()
        {
            var rfp = new RangefinderProperties
            {
                AngularResolution = Constants.PiOver2,
                AngularRange = Constants.Pi,
                MaxRange = 2f,
                OriginPose = new Pose(new Vector2(), Constants.PiOver2)
            };
            Assert.That(rfp.BeamToVectorInRobotTransformation(1, 0).EqualsRelatively(new Vector2(1, 0), 0.001));
            Assert.That(rfp.BeamToVectorInRobotTransformation(1, 1).EqualsRelatively(new Vector2(0, 1), 0.001));
            Assert.That(rfp.BeamToVectorInRobotTransformation(1, 2).EqualsRelatively(new Vector2(-1, 0), 0.001));

	        rfp.OriginPose = new Pose(new Vector2(1, 2), 0);
			Assert.That(rfp.BeamToVectorInRobotTransformation(1, 0).EqualsRelatively(new Vector2(1, 1), 0.001));
			Assert.That(rfp.BeamToVectorInRobotTransformation(1, 1).EqualsRelatively(new Vector2(2, 2), 0.001));
			Assert.That(rfp.BeamToVectorInRobotTransformation(1, 2).EqualsRelatively(new Vector2(1, 3), 0.001));

			rfp.OriginPose = new Pose(new Vector2(1, 2), Constants.PiOver4);
			Assert.That(rfp.BeamToVectorInRobotTransformation(1, 0).EqualsRelatively(new Vector2(1 + (float)Constants.Sqrt1Over2, 2 - (float)Constants.Sqrt1Over2), 0.001));
			Assert.That(rfp.BeamToVectorInRobotTransformation(1, 1).EqualsRelatively(new Vector2(1 + (float)Constants.Sqrt1Over2, 2 + (float)Constants.Sqrt1Over2), 0.001));
			Assert.That(rfp.BeamToVectorInRobotTransformation(1, 2).EqualsRelatively(new Vector2(1 - (float)Constants.Sqrt1Over2, 2 + (float)Constants.Sqrt1Over2), 0.001));
        }

        [Test]
        public void DistanceToNearestObstacle()
        {
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(1.5f, 0.5f)), Is.EqualTo(0)); //center of occupied cell
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(1.9f, 0.9f)), Is.EqualTo(0)); //inside occupied cell
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(0.5f, 0.5f)), Is.EqualTo(0.4).Within(1e-5));
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(1.5f, 1.5f)), Is.EqualTo(0.4).Within(1e-5));
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(0.5f, 1.5f)), Is.EqualTo(Math.Sqrt(2 * 1 * 1) - 0.6).Within(1e-5));
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(1.7f, 1.5f)), Is.EqualTo(0.2).Within(1e-5)); //between two equidistant occupied cells

            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(0.6f, 0.5f)), Is.EqualTo(0.3).Within(1e-5));
            Assert.That(_lfmm.DistanceToNearestObstacle(new Vector2(0.95f, 0.5f)), Is.EqualTo(0).Within(1e-5));
        }

        [Test]
        public void DistanceToNearestObstacleNoObstacle()
        {
            var lfmm = new LikelihoodFieldMeasurementModel
            (
                new OccupancyGrid(new[,] { { false, false, false }, { false, false, false} }, 1),
                new RangefinderProperties { AngularResolution = Constants.PiOver2, AngularRange = Constants.Pi, MaxRange = 2f, OriginPose = new Pose(new Vector2(), 3 * Constants.PiOver2) },
                0.1f, 0.7f, 0.3f
            );

            Assert.That(float.IsPositiveInfinity(lfmm.DistanceToNearestObstacle(new Vector2(1, 1))));
        }
    }
}