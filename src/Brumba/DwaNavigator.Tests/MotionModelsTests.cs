using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class MotionModelsTests
    {
        [Test]
        public void CircleMotionModelPredictPoseDeltaTest()
        {
            var cmm = new CircleMotionModel();

            Assert.That(cmm.PredictPoseDelta(v: new Velocity(2, Constants.PiOver2 / 0.1), dt: 0.1),
                Is.EqualTo(new Pose(new Vector2((float)(2 / (Constants.PiOver2 / 0.1)), (float)(2 / (Constants.PiOver2 / 0.1))), Constants.PiOver2)));

            var pp = cmm.PredictPoseDelta(new Velocity(2, Constants.PiOver2 / 0.1), 0.5);
            Assert.That(pp.Position.EqualsWithError(
                new Vector2((float) (2/(Constants.PiOver2/0.1)), (float) (2/(Constants.PiOver2/0.1))), 1e-7));
            Assert.That(pp.Bearing, Is.EqualTo(5 * Constants.PiOver2));

            Assert.That(cmm.PredictPoseDelta(new Velocity(2, -Constants.PiOver2 / 0.1), 0.1),
                Is.EqualTo(new Pose(new Vector2((float)(2 / (Constants.PiOver2 / 0.1)), (float)(2 / (-Constants.PiOver2 / 0.1))), -Constants.PiOver2)));

            Assert.That(cmm.PredictPoseDelta(new Velocity(0, Constants.PiOver2 / 0.1), 0.1),
                Is.EqualTo(new Pose(new Vector2(), Constants.PiOver2)));

            Assert.That(cmm.PredictPoseDelta(new Velocity(2, Constants.PiOver2 / 0.1), 0),
                Is.EqualTo(new Pose(new Vector2(), 0)));
        }

        [Test]
        public void CircleMotionModelCenterTest()
        {
            var cmm = new CircleMotionModel();

            Assert.That(cmm.GetCenter(new Velocity(6, 2)), Is.EqualTo(new Vector2(0, 3)));
            Assert.That(cmm.GetCenter(new Velocity(0, 2)), Is.EqualTo(new Vector2()));
        }

        [Test]
        public void LineMotionModelTest()
        {
            var lmm = new LineMotionModel();

            Assert.That(lmm.PredictPoseDelta(new Velocity(2, 0), dt: 0.1), Is.EqualTo(new Pose(new Vector2((float)(2 * 0.1), 0), 0)));

            Assert.That(lmm.PredictPoseDelta(new Velocity(-2, 0), 0.1), Is.EqualTo(new Pose(new Vector2((float)(-2 * 0.1), 0), 0)));
            
            Assert.That(lmm.PredictPoseDelta(new Velocity(0, 0), 0.1), Is.EqualTo(new Pose(new Vector2(), 0)));
            Assert.That(lmm.PredictPoseDelta(new Velocity(2, 0), 0), Is.EqualTo(new Pose(new Vector2(), 0)));
        }
    }
}