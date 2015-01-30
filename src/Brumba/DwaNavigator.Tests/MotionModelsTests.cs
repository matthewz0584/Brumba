using Brumba.Common;
using Brumba.Utils;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class MotionModelsTests
    {
        [Test]
        public void CircleMotionModelPredictPoseDelta()
        {
            Assert.That(new CirclularMotionModel(new Velocity(2, Constants.PiOver2 / 0.1)).PredictPoseDelta(dt: 0.1),
                Is.EqualTo(new Pose(new Vector2((float)(2 / (Constants.PiOver2 / 0.1)), (float)(2 / (Constants.PiOver2 / 0.1))), Constants.PiOver2)));

            var pp = new CirclularMotionModel(new Velocity(2, Constants.PiOver2 / 0.1)).PredictPoseDelta(0.5);
            Assert.That(pp.Position.EqualsWithError(
                new Vector2((float) (2/(Constants.PiOver2/0.1)), (float) (2/(Constants.PiOver2/0.1))), 1e-7));
            Assert.That(pp.Bearing, Is.EqualTo(5 * Constants.PiOver2));

            Assert.That(new CirclularMotionModel(new Velocity(2, -Constants.PiOver2 / 0.1)).PredictPoseDelta(0.1),
                Is.EqualTo(new Pose(new Vector2((float)(2 / (Constants.PiOver2 / 0.1)), (float)(2 / (-Constants.PiOver2 / 0.1))), -Constants.PiOver2)));

            Assert.That(new CirclularMotionModel(new Velocity(0, Constants.PiOver2 / 0.1)).PredictPoseDelta(0.1),
                Is.EqualTo(new Pose(new Vector2(), Constants.PiOver2)));
        }

        [Test]
        public void CircleMotionModelGetCenter()
        {
            Assert.That(new CirclularMotionModel(new Velocity(6, 2)).Center, Is.EqualTo(new Vector2(0, 3)));
            Assert.That(new CirclularMotionModel(new Velocity(6, -2)).Center, Is.EqualTo(new Vector2(0, -3)));
            Assert.That(new CirclularMotionModel(new Velocity(0, 2)).Center, Is.EqualTo(new Vector2()));
        }

        [Test]
        public void CircleMotionModelGetRadius()
        {
            Assert.That(new CirclularMotionModel(new Velocity(6, 2)).Radius, Is.EqualTo(3));
            Assert.That(new CirclularMotionModel(new Velocity(6, -2)).Radius, Is.EqualTo(-3));
            Assert.That(new CirclularMotionModel(new Velocity(0, 2)).Radius, Is.EqualTo(0));
        }

        [Test]
        public void LineMotionModel()
        {
            Assert.That(new LinearMotionModel(2).PredictPoseDelta(dt: 0.1), Is.EqualTo(new Pose(new Vector2((float)(2 * 0.1), 0), 0)));

            Assert.That(new LinearMotionModel(-2).PredictPoseDelta(0.1), Is.EqualTo(new Pose(new Vector2((float)(-2 * 0.1), 0), 0)));
            
            Assert.That(new LinearMotionModel(0).PredictPoseDelta(0.1), Is.EqualTo(new Pose(new Vector2(), 0)));
        }
    }
}