using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.McLrfLocalizer.Tests
{
    [TestFixture]
    public class RangeFinderPropertiesTests
    {
        [Test]
        public void BeamToVectorInRobotTransformation()
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
        public void PreprocessMeasurements()
        {
            var rfp = new RangefinderProperties
            {
                AngularResolution = Constants.PiOver2,
                AngularRange = Constants.Pi,
                MaxRange = 2f,
                OriginPose = new Pose(new Vector2(1, 2), Constants.PiOver4)
            };

            Assert.That(rfp.PreprocessMeasurements(new[] {0.5f, 1.2f, 0.1f}), Is.EquivalentTo(new[]
                {
                    rfp.BeamToVectorInRobotTransformation(0.5f, 0),
                    rfp.BeamToVectorInRobotTransformation(1.2f, 1),
                    rfp.BeamToVectorInRobotTransformation(0.1f, 2)
                }));
            Assert.That(rfp.PreprocessMeasurements(new[] { 0.5f, 2, 2 }), Is.EquivalentTo(new[] { rfp.BeamToVectorInRobotTransformation(0.5f, 0) }));
            Assert.That(rfp.PreprocessMeasurements(new[] { 2f, 2, 2 }), Is.Empty);
        }

        [Test]
        public void Sparsify()
        {
            var srcRp = new RangefinderProperties
            {
                MaxRange = 2,
                OriginPose = new Pose(new Vector2(1, 2), 3),
                AngularRange = 30,
                AngularResolution = 4
            };
            var sparsifiedRp = srcRp.Sparsify(3);

            Assert.That(sparsifiedRp.MaxRange, Is.EqualTo(srcRp.MaxRange));
            Assert.That(sparsifiedRp.OriginPose, Is.EqualTo(srcRp.OriginPose));
            Assert.That(sparsifiedRp.AngularRange, Is.EqualTo(srcRp.AngularRange));
            Assert.That(sparsifiedRp.AngularResolution, Is.EqualTo(4 * 3));
            Assert.That(sparsifiedRp.AngularResolution * 2, Is.LessThanOrEqualTo(srcRp.AngularRange));
            Assert.That(sparsifiedRp.AngularResolution * 3, Is.GreaterThanOrEqualTo(srcRp.AngularRange));

            srcRp.AngularResolution = 3;
            sparsifiedRp = srcRp.Sparsify(5);

            Assert.That(sparsifiedRp.AngularResolution, Is.EqualTo(3 * 2));
            Assert.That(sparsifiedRp.AngularResolution * 4, Is.LessThanOrEqualTo(srcRp.AngularRange));
            Assert.That(sparsifiedRp.AngularResolution * 5, Is.GreaterThanOrEqualTo(srcRp.AngularRange));
        }
    }
}