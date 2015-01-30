using Brumba.Common;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class DistanceToTargetEvaluatorTests
    {
        [Test]
        public void MergeSequentialPoseDeltas()
        {
            Assert.That(NextPoseEvaluator.MergeSequentialPoseDeltas(new Pose(new Vector2(1, 2), 0), new Pose(new Vector2(2, -3), 0)),
                Is.EqualTo(new Pose(new Vector2(3, -1), 0)));

            Assert.That(NextPoseEvaluator.MergeSequentialPoseDeltas(new Pose(new Vector2(), Constants.PiOver4), new Pose(new Vector2(), -Constants.Pi / 8)),
                Is.EqualTo(new Pose(new Vector2(), Constants.Pi / 8)));

            Assert.That(NextPoseEvaluator.MergeSequentialPoseDeltas(new Pose(new Vector2(1, 2), Constants.PiOver2), new Pose(new Vector2(2, 0), -Constants.PiOver4)).Position.
                EqualsWithError(new Vector2(1, 4), 1e-7));
            Assert.That(NextPoseEvaluator.MergeSequentialPoseDeltas(new Pose(new Vector2(1, 2), Constants.PiOver2), new Pose(new Vector2(2, 0), -Constants.PiOver4)).Bearing,
                Is.EqualTo(Constants.PiOver4));
        }

        [Test]
        public void ChooseMotionModel()
        {
            Assert.That(NextPoseEvaluator.ChooseMotionModel(new Velocity(10, 0)), Is.TypeOf<LinearMotionModel>());
            Assert.That(NextPoseEvaluator.ChooseMotionModel(new Velocity(0, 10)), Is.TypeOf<CirclularMotionModel>());
            Assert.That(NextPoseEvaluator.ChooseMotionModel(new Velocity(0, 0.001)), Is.TypeOf<LinearMotionModel>());
        }

        [Test]
        public void PredictNextPose()
        {
            var npe = new NextPoseEvaluator(new Pose(new Vector2(1, 2), Constants.PiOver2), 0.1);

            Assert.That(npe.PredictNextPose(new Velocity(10, 0)).Position.EqualsWithError(new Vector2(1, 3), 1e-7));
            Assert.That(npe.PredictNextPose(new Velocity(10, 0)).Bearing, Is.EqualTo(Constants.PiOver2));

            Assert.That(npe.PredictNextPose(new Velocity(10, Constants.Pi * 10)).Position.EqualsWithError(new Vector2(1 - 2/(float)Constants.Pi, 2), 1e-7));
            Assert.That(npe.PredictNextPose(new Velocity(10, Constants.Pi * 10)).Bearing, Is.EqualTo(3 * Constants.PiOver2));
        }

        [Test]
        public void Evaluate()
        {
            var dte = new DistanceToTargetEvaluator(new Pose(new Vector2(), 0), new Vector2(5, 0), 2, 0.1);

            Assert.That(dte.Evaluate(new Velocity(2, 0)), Is.EqualTo(1).Within(1e7));
            Assert.That(dte.Evaluate(new Velocity(-2, 0)), Is.EqualTo(0).Within(1e7));

            Assert.That(dte.Evaluate(new Velocity(1, 0)), Is.GreaterThan(dte.Evaluate(new Velocity(0.5, 0))));
            Assert.That(dte.Evaluate(new Velocity(1, 1)), Is.EqualTo(dte.Evaluate(new Velocity(1, -1))));
        }
    }
}