using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class AngleToTargetPredictorTests
    {
        [Test]
        public void Acceptance()
        {
            var atp = new AngleToTargetPredictor(target: new Vector2(10, 0), maxLinearAcceleration: 2,
                maxAngularAcceleration: 2, dt: 0.1);

            Assert.That(atp.Predict(pose: new Pose(new Vector2(0, 0), 0), velocity: new Pose(new Vector2(1, 0), 0)), Is.EqualTo(0));

            Assert.That();
        }
    }
}