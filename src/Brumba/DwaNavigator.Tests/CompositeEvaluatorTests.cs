using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class CompositeEvaluatorTests
    {
        [Test]
        public void Evaluate()
        {
            var ce = new CompositeEvaluator(evaluatorWeights: new Dictionary<IVelocityEvaluator, double>
            {
                { Substitute.For<IVelocityEvaluator>(), 0.2 },
                { Substitute.For<IVelocityEvaluator>(), 0.3 },
                { Substitute.For<IVelocityEvaluator>(), 0.5 }
            });

            ce.EvaluatorWeights.Keys.ToList()[0].Evaluate(Arg.Any<Velocity>()).Returns(0.5);
            ce.EvaluatorWeights.Keys.ToList()[1].Evaluate(Arg.Any<Velocity>()).Returns(0.5);
            ce.EvaluatorWeights.Keys.ToList()[2].Evaluate(Arg.Any<Velocity>()).Returns(0.5);

            Assert.That(ce.Evaluate(new Velocity()), Is.EqualTo(0.5));

            ce.EvaluatorWeights.Keys.ToList()[0].Evaluate(Arg.Any<Velocity>()).Returns(1);
            ce.EvaluatorWeights.Keys.ToList()[1].Evaluate(Arg.Any<Velocity>()).Returns(0);
            ce.EvaluatorWeights.Keys.ToList()[2].Evaluate(Arg.Any<Velocity>()).Returns(0);
            
            Assert.That(ce.Evaluate(new Velocity()), Is.EqualTo(0.2));
        }
    }
}