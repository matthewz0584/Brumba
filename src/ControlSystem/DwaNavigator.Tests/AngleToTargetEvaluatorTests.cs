using Brumba.Common;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NSubstitute;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class AngleToTargetEvaluatorTests
    {
        [Test]
        public void CalculateVelocityAfterAngularDeceleration()
        {
            var velocityPredictor = Substitute.For<IVelocityPredictor>();
            var atp = new AngleToTargetEvaluator(pose: new Pose(), target: new Vector2(), dt: 0.1, velocityPredictor: velocityPredictor);

            Assert.That(atp.CalculateVelocityAfterAngularDeceleration(new Velocity(2, 0)), Is.EqualTo(new Velocity(2, 0)));
            velocityPredictor.DidNotReceive().PredictVelocity(Arg.Any<Velocity>(), Arg.Any<Vector2>(), Arg.Any<double>());

            velocityPredictor.PredictVelocity(Arg.Any<Velocity>(), Arg.Any<Vector2>(), Arg.Any<double>()).Returns(ci => new Velocity(2, -1));
            Assert.That(atp.CalculateVelocityAfterAngularDeceleration(new Velocity(2, 1)), Is.EqualTo(new Velocity(2, 0)));
            velocityPredictor.Received().PredictVelocity(new Velocity(2, 1), new Vector2(1, -1), 0.1 / 2);

            Assert.That(atp.CalculateVelocityAfterAngularDeceleration(new Velocity(2, -2)), Is.EqualTo(new Velocity(2, -1)));
            velocityPredictor.Received().PredictVelocity(new Velocity(2, -2), new Vector2(-1, 1), 0.1 / 2);
        }

        [Test]
        public void GetAngleToTarget()
        {
            var atp = new AngleToTargetEvaluator(new Pose(), new Vector2(10, 0), 1, Substitute.For<IVelocityPredictor>());

            Assert.That(atp.GetAngleToTarget(pose: new Pose(new Vector2(0, 0), 0)), Is.EqualTo(0));

            Assert.That(atp.GetAngleToTarget(new Pose(new Vector2(0, 0), Constants.PiOver2)), Is.EqualTo(Constants.PiOver2));

            Assert.That(atp.GetAngleToTarget(new Pose(new Vector2(5, 5), 0)), Is.EqualTo(Constants.PiOver4).Within(1e-7));

            Assert.That(atp.GetAngleToTarget(new Pose(new Vector2(5, 5), -Constants.PiOver4)), Is.EqualTo(0));
        }

        [Test]
        public void Evaluate()
        {
            var atp = new AngleToTargetEvaluator(new Pose(new Vector2(0, 2), 0), new Vector2(2, 2), 0.1, new DiffDriveVelocitySpaceGenerator(0.01, 0.01, 0.01, 0.1, 100, 1, 0, 0.1));

            Assert.That(atp.Evaluate(v: new Velocity(2, 0)), Is.EqualTo(1));

            Assert.That(atp.Evaluate(new Velocity(0, Constants.PiOver2 / 0.1)), Is.EqualTo(0.5));

            var ev1 = atp.Evaluate(new Velocity(2, Constants.PiOver2 / 0.1));
            Assert.That(ev1, Is.GreaterThan(0.25).And.LessThan(0.5));

            Assert.That(atp.Evaluate(new Velocity(2, -Constants.PiOver2 / 0.1)), Is.EqualTo(ev1).Within(1e-7));

            atp = new AngleToTargetEvaluator(new Pose(new Vector2(0, 1), 0), new Vector2(2, 0), 0.1, Substitute.For<IVelocityPredictor>());

            Assert.That(atp.Evaluate(new Velocity(5, 0)), Is.EqualTo(1 - 0.25).Within(1e-7));
        }
    }
}