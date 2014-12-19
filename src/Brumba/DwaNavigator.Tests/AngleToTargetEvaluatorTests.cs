using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class AngleToTargetEvaluatorTests
    {
        [Test]
        public void PredictPoseDeltaLineAfterLine()
        {
            var atp = new AngleToTargetEvaluator(pose: new Pose(), target: new Vector2(), maxAngularAcceleration: 1, dt: 0.1);

            //Robot moves on line over the first time delta, thus there is no need to damp angular velocity over the second delta.
            //Hence, robot proceeds in moving on line over the second time delta.
            Assert.That(atp.PredictPoseDelta(v: new Velocity(2, 0)), Is.EqualTo(new Pose(new Vector2(2 * 0.2f, 0), 0)));
        }

        [Test]
        public void PredictPoseDeltaLineAfterCircle()
        {
            var atp = new AngleToTargetEvaluator(new Pose(), new Vector2(), Constants.PiOver2 / 0.1 / 0.1, 0.1);

            //Robot moves on circle over the first time delta (gets over exactly quarter of a circle, which determines the angular velocity: Constants.PiOver2 / 0.1).
            //Then it decelerates angularly and proceeds in straight trajectory over the cesond time delta (that determines the value of max ang deceleration: 
            //it should totally damp given angular velocity). The linear velocity determines the radius of a circle and the distance over 
            //the straight piece of trajectory. This installation allows easily predict robot's position.
            var predictedPoseDelta = atp.PredictPoseDelta(new Velocity(2, Constants.PiOver2 / 0.1));
            Assert.That(predictedPoseDelta.Position.
                EqualsWithError(new Vector2((float)(2 / (Constants.PiOver2 / 0.1)), (float)(2 / (Constants.PiOver2 / 0.1) + 2 * 0.1)), 1e-7));
            Assert.That(predictedPoseDelta.Bearing, Is.EqualTo(Constants.PiOver2));
        }

        [Test]
        public void PredictPoseDeltaCircleAfterCircle()
        {
            //Robot moves on circle over the first time delta (gets over exactly quarter of a circle, which determines the angular velocity: Constants.PiOver2 / 0.1).
            //Then it decelerates angularly but not enough to totally damp the angular velocity out, so it proceeds in circle with greater radius over the cesond time delta
            //(that determines the value of max ang deceleration: it multiplied by time delta equals half of the initial angular velocity).
            //The calculation of resultant position involves simple trigonometry (radius of the second circle is twice the first one).
            var atp = new AngleToTargetEvaluator(new Pose(), new Vector2(), Constants.PiOver2 / 2 / 0.1 / 0.1, 0.1);

            var predictedPoseDelta = atp.PredictPoseDelta(new Velocity(2, Constants.PiOver2 / 0.1));
            Assert.That(predictedPoseDelta.Position.
                EqualsWithError(new Vector2((float)(2 / (Constants.PiOver2 / 0.1) - 2 / (Constants.PiOver2 / 0.1 / 2) * (1 - Constants.Sqrt1Over2)),
                                            (float)(2 / (Constants.PiOver2 / 0.1) + 2 / (Constants.PiOver2 / 0.1 / 2) * Constants.Sqrt1Over2)), 1e-7));
            Assert.That(predictedPoseDelta.Bearing, Is.EqualTo(3 * Constants.PiOver4));
        }

        [Test]
        public void GetAngleToTarget()
        {
            var atp = new AngleToTargetEvaluator(new Pose(), new Vector2(10, 0), 1, 1);

            Assert.That(atp.GetAngleToTarget(pose: new Pose(new Vector2(0, 0), 0)), Is.EqualTo(0));

            Assert.That(atp.GetAngleToTarget(new Pose(new Vector2(0, 0), Constants.PiOver2)), Is.EqualTo(Constants.PiOver2));

            Assert.That(atp.GetAngleToTarget(new Pose(new Vector2(5, 5), 0)), Is.EqualTo(Constants.PiOver4).Within(1e-7));

            Assert.That(atp.GetAngleToTarget(new Pose(new Vector2(5, 5), -Constants.PiOver4)), Is.EqualTo(0));
        }

        [Test]
        public void Evaluate()
        {
            var atp = new AngleToTargetEvaluator(new Pose(new Vector2(0, 2), 0), new Vector2(2, 2), Constants.PiOver2 / 0.1 / 0.1, 0.1);

            Assert.That(atp.Evaluate(v: new Velocity(2, 0)), Is.EqualTo(1));

            Assert.That(atp.Evaluate(new Velocity(0, 0.75 * Constants.Pi / 0.1)), Is.EqualTo(0));

            var ev1 = atp.Evaluate(new Velocity(2, Constants.PiOver2 / 0.1));
            Assert.That(ev1, Is.GreaterThan(0.25).And.LessThan(0.5));

            Assert.That(atp.Evaluate(new Velocity(2, -Constants.PiOver2 / 0.1)), Is.EqualTo(ev1));
        }
    }
}