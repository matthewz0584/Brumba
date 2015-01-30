using Brumba.Common;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class SpeedEvaluatorTests
    {
        [Test]
        public void Evaluate()
        {
            var se = new SpeedEvaluator(robotMaxSpeed: 2d);

            Assert.That(se.Evaluate(new Velocity()), Is.EqualTo(0));
            Assert.That(se.Evaluate(new Velocity(1, 0)), Is.EqualTo(0.5));
            Assert.That(se.Evaluate(new Velocity(1, 100)), Is.EqualTo(0.5));
            Assert.That(se.Evaluate(new Velocity(2, 0)), Is.EqualTo(1));
        }
    }
}