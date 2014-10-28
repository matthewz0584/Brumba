using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.McLrfLocalizer;
using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;
using NSubstitute;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class DwaProblemOptimizerTests
    {
        [Test]
        public void Optimize()
        {
            var dwan = new DwaProblemOptimizer(
                dynamicWindowGenerator: Substitute.For<IDynamicWindowGenerator>(),
                velocityEvaluator: Substitute.For<IVelocityEvaluator>());

            var velocityWheelAccRel = new Dictionary<Velocity, Vector2>
            {
                {new Velocity(1, 2), new Vector2(0.1f, 0.2f)},
                {new Velocity(3, 4), new Vector2(0.3f, 0.4f)},
                {new Velocity(5, 6), new Vector2(0.5f, 0.6f)}
            };
            dwan.DynamicWindowGenerator.Generate(Arg.Any<Velocity>()).Returns(velocityWheelAccRel);
            dwan.VelocityEvaluator.Evaluate(Arg.Any<Velocity>()).Returns(ci => ci.Arg<Velocity>().Angular);

            Assert.That(dwan.Optimize(velocity: new Pose(new Vector2(3, 4), 2)), Is.EqualTo(new Vector2(0.5f, 0.6f)));

            dwan.DynamicWindowGenerator.Received().Generate(new Velocity(5, 2));
            dwan.VelocityEvaluator.Received(3).Evaluate(Arg.Is<Velocity>(vel => velocityWheelAccRel.Keys.Contains(vel)));
        }

        [Test]
        public void OptimizePrunesNegativeLinearVelocities()
        {
            var dwan = new DwaProblemOptimizer(
                dynamicWindowGenerator: Substitute.For<IDynamicWindowGenerator>(),
                velocityEvaluator: Substitute.For<IVelocityEvaluator>());

            var velocityWheelAccRel = new Dictionary<Velocity, Vector2>
            {
                {new Velocity(1, 2), new Vector2(0.1f, 0.2f)},
                {new Velocity(3, 4), new Vector2(0.3f, 0.4f)},
                {new Velocity(-5, 6), new Vector2(0.5f, 0.6f)}
            };
            dwan.DynamicWindowGenerator.Generate(Arg.Any<Velocity>()).Returns(velocityWheelAccRel);
            dwan.VelocityEvaluator.Evaluate(Arg.Any<Velocity>()).Returns(ci => ci.Arg<Velocity>().Angular);

            Assert.That(dwan.Optimize(velocity: new Pose(new Vector2(3, 4), 2)), Is.EqualTo(new Vector2(0.3f, 0.4f)));

            dwan.VelocityEvaluator.DidNotReceive().Evaluate(new Velocity(-5, 6));
        }

        //[Test]
        //public void OptimizePrunesNonAdmissibleVelocities()
        //{
        //    var dwan = new DwaProblemOptimizer(
        //        dynamicWindowGenerator: Substitute.For<IDynamicWindowGenerator>(),
        //        velocityEvaluator: Substitute.For<IVelocityEvaluator>(),
        //        linearDecelerationMax: 1.0d);

        //    var velocityWheelAccRel = new Dictionary<Velocity, Vector2>()
        //    {
        //        {new Velocity(1, 2), new Vector2(0.1f, 0.2f)},
        //        {new Velocity(3, 4), new Vector2(0.3f, 0.4f)},
        //        {new Velocity(-5, 6), new Vector2(0.5f, 0.6f)}
        //    };
        //    dwan.DynamicWindowGenerator.Generate(Arg.Any<Velocity>()).Returns(velocityWheelAccRel);
        //    dwan.VelocityEvaluator.Evaluate(Arg.Any<Velocity>()).Returns(ci => ci.Arg<Velocity>().Angular);

        //    Assert.That(dwan.Optimize(velocity: new Pose(new Vector2(3, 4), 2)), Is.EqualTo(new Vector2(0.3f, 0.4f)));

        //    dwan.VelocityEvaluator.DidNotReceive().Evaluate(new Velocity(-5, 6));
        //}

        //[Test]
        //Smoothed
        //Smoothing does not allow to just drop bad (negative and not admissible) velocities, it makes them stay and have a value in order to be smoothed
    }
}