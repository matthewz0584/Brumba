using System.Linq;
using Brumba.WaiterStupid;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Xna.Framework;
using NSubstitute;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class DwaProblemOptimizerTests
    {
        [Test]
        public void FindOptimalVelocity()
        {
            var vwa = new [,]
            {
                {new VelocityAcceleration(), new VelocityAcceleration(new Velocity(1, 2), new Vector2(0.1f, 0.2f)), new VelocityAcceleration()},
                {new VelocityAcceleration(), new VelocityAcceleration(new Velocity(3, 4), new Vector2(0.3f, 0.4f)), new VelocityAcceleration()},
                {new VelocityAcceleration(), new VelocityAcceleration(new Velocity(5, 6), new Vector2(0.5f, 0.6f)), new VelocityAcceleration()}
            };

            //Because NSubstitute has error with multidimensional return values, special fake class used
            var vssg = new FakeVelocitySearchSpaceGenerator(vwa);

            var dwan = new DwaProblemOptimizer(
                velocitySearchSpaceGenerator: vssg,
                velocityEvaluator: Substitute.For<IVelocityEvaluator>());

            dwan.VelocityEvaluator.Evaluate(Arg.Any<Velocity>()).Returns(ci => ci.Arg<Velocity>().Angular);

            Assert.That(dwan.FindOptimalVelocity(velocity: new Pose(new Vector2(3, 4), 2)), Is.EqualTo(new Vector2(0.5f, 0.6f)));

            Assert.That(vssg.ReceivedGenerate(new Velocity(5, 2)));
            dwan.VelocityEvaluator.Received(9).Evaluate(Arg.Is<Velocity>(vel => vwa.Cast<VelocityAcceleration>().Any(va => va.Velocity.Equals(vel))));
        }

        [Test]
        public void FindOptimalVelocitySmoothesEvaluationValues()
        {
            var vwa = new[,]
            {
                {new VelocityAcceleration(), new VelocityAcceleration(new Velocity(1, 2), new Vector2(0.1f, 0.2f)), new VelocityAcceleration()},
                {new VelocityAcceleration(), new VelocityAcceleration(new Velocity(3, 4), new Vector2(0.3f, 0.4f)), new VelocityAcceleration()},
                {new VelocityAcceleration(), new VelocityAcceleration(new Velocity(5, 1), new Vector2(0.5f, 0.6f)), new VelocityAcceleration()}
            };

            //Because NSubstitute has error with multidimensional return values, special fake class used
            var vssg = new FakeVelocitySearchSpaceGenerator(vwa);

            var dwan = new DwaProblemOptimizer(
                velocitySearchSpaceGenerator: vssg,
                velocityEvaluator: Substitute.For<IVelocityEvaluator>());

            dwan.VelocityEvaluator.Evaluate(Arg.Any<Velocity>()).Returns(ci => ci.Arg<Velocity>().Angular);

            Assert.That(dwan.FindOptimalVelocity(velocity: new Pose(new Vector2(3, 4), 2)), Is.EqualTo(new Vector2(0.1f, 0.2f)));

            dwan.VelocityEvaluator.Received(9).Evaluate(Arg.Is<Velocity>(vel => vwa.Cast<VelocityAcceleration>().Any(va => va.Velocity.Equals(vel))));
        }

        [Test]
        public void FindOptimalVelocityPrunesNegativeLinearVelocities()
        {
            var vwa = new[,]
            {
                {new VelocityAcceleration(), new VelocityAcceleration(new Velocity(1, 2), new Vector2(0.1f, 0.2f)), new VelocityAcceleration()},
                {new VelocityAcceleration(), new VelocityAcceleration(new Velocity(3, 8), new Vector2(0.3f, 0.4f)), new VelocityAcceleration()},
                {new VelocityAcceleration(), new VelocityAcceleration(new Velocity(-5, 16), new Vector2(0.5f, 0.6f)), new VelocityAcceleration()}
            };

            //Because NSubstitute has error with multidimensional return values, special fake class used
            var vssg = new FakeVelocitySearchSpaceGenerator(vwa);

            var dwan = new DwaProblemOptimizer(
                velocitySearchSpaceGenerator: vssg,
                velocityEvaluator: Substitute.For<IVelocityEvaluator>());

            dwan.VelocityEvaluator.Evaluate(Arg.Any<Velocity>()).Returns(ci => ci.Arg<Velocity>().Angular);

            Assert.That(dwan.FindOptimalVelocity(velocity: new Pose(new Vector2(3, 4), 2)), Is.EqualTo(new Vector2(0.3f, 0.4f)));

            dwan.VelocityEvaluator.Received(8).Evaluate(Arg.Is<Velocity>(vel => vwa.Cast<VelocityAcceleration>().Any(va => va.Velocity.Equals(vel))));
            dwan.VelocityEvaluator.DidNotReceive().Evaluate(new Velocity(-5, 6));
        }

        [Test]
        public void SmoothMatrix()
        {
            var dwan = new DwaProblemOptimizer(
                            velocitySearchSpaceGenerator: Substitute.For<IVelocitySearchSpaceGenerator>(),
                            velocityEvaluator: Substitute.For<IVelocityEvaluator>());

            var m = DenseMatrix.OfArray(new double[,]
                {
                    {1, 2, 3, 0},
                    {4, 5, 6, 0},
                    {7, 8, 9, 0},
                    {0, 0, 0, 0}
                });
            var smoothedM = dwan.Smooth(m);
            
            Assert.That(smoothedM[0, 0], Is.EqualTo(1));
            Assert.That(smoothedM[0, 1], Is.EqualTo(2));
            Assert.That(smoothedM[0, 2], Is.EqualTo(3));
            Assert.That(smoothedM[0, 3], Is.EqualTo(0));

            Assert.That(smoothedM[1, 0], Is.EqualTo(4));
            Assert.That(smoothedM[1, 1], Is.EqualTo(80d / 16));
            Assert.That(smoothedM[1, 2], Is.EqualTo(68d / 16));
            Assert.That(smoothedM[1, 3], Is.EqualTo(0));

            Assert.That(smoothedM[2, 0], Is.EqualTo(7));
            Assert.That(smoothedM[2, 1], Is.EqualTo(84d / 16));
            Assert.That(smoothedM[2, 2], Is.EqualTo(69d / 16));
            Assert.That(smoothedM[2, 3], Is.EqualTo(0));

            Assert.That(smoothedM[3, 0], Is.EqualTo(0));
            Assert.That(smoothedM[3, 1], Is.EqualTo(0));
            Assert.That(smoothedM[3, 2], Is.EqualTo(0));
            Assert.That(smoothedM[3, 3], Is.EqualTo(0));
        }

        [Test]
        public void SmoothMatrixSkipsNegativeCells()
        {
            var dwan = new DwaProblemOptimizer(
                            velocitySearchSpaceGenerator: Substitute.For<IVelocitySearchSpaceGenerator>(),
                            velocityEvaluator: Substitute.For<IVelocityEvaluator>());

            var m = DenseMatrix.OfArray(new double[,]
                {
                    {1, -2, 3, 0},
                    {4, 5, 6, 0},
                    {7, 8, -9, 0},
                    {0, 0, 0, 0}
                });
            var smoothedM = dwan.Smooth(m);

            Assert.That(smoothedM[1, 0], Is.EqualTo(4));
            Assert.That(smoothedM[1, 1], Is.EqualTo(67d / 13));
            Assert.That(smoothedM[1, 2], Is.EqualTo(48d / 13));
            Assert.That(smoothedM[1, 3], Is.EqualTo(0));

            Assert.That(smoothedM[2, 0], Is.EqualTo(7));
            Assert.That(smoothedM[2, 1], Is.EqualTo(66d / 14));
            Assert.That(smoothedM[2, 2], Is.EqualTo(-9));
            Assert.That(smoothedM[2, 3], Is.EqualTo(0));
        }
    }

    public class FakeVelocitySearchSpaceGenerator : IVelocitySearchSpaceGenerator
    {
        private readonly VelocityAcceleration[,] _vas;
        private Velocity _center;

        public FakeVelocitySearchSpaceGenerator(VelocityAcceleration[,] vas)
        {
            _vas = vas;
        }

        public VelocityAcceleration[,] Generate(Velocity center)
        {
            _center = center;
            return _vas;
        }

        public bool ReceivedGenerate(Velocity center)
        {
            return _center.Equals(center);
        }
    }
}