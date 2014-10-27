using System;
using System.Linq;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class DynamicDiamondGeneratorTests
    {
        [Test]
        public void AccelerationMax()
        {
            var ddc = new DynamicDiamondGenerator(wheelAngularAccelerationMax: 0.5d, wheelRadius: 1.5d, wheelBase: 3d, dt: 0.1);

            Assert.That(ddc.AccelerationMax, Is.EqualTo(new Velocity(1.5d / 2 * (0.5 + 0.5), 1.5d / 3d * (0.5 + 0.5))));
        }

        [Test]
        public void VelocityStep()
        {
            var ddc = new DynamicDiamondGenerator(0.5d, 1.5d, 3d, 0.1d);

            Assert.That(ddc.VelocityStep, Is.EqualTo(new Velocity(1.5d / 2 * (0.5 + 0.5) * 0.1 / DynamicDiamondGenerator.STEPS_NUMBER, 1.5d / 3d * (0.5 + 0.5) * 0.1 / DynamicDiamondGenerator.STEPS_NUMBER)));
        }

        [Test]
        public void Generate()
        {
            var ddc = new DynamicDiamondGenerator(0.5d, 1.5d, 3d, 0.1);

            Assert.That(ddc.Generate(new Velocity(3, 0)).Count(), Is.EqualTo((2 * DynamicDiamondGenerator.STEPS_NUMBER + 1) * (2 * DynamicDiamondGenerator.STEPS_NUMBER + 1)));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Keys.Min(v => v.Linear), Is.EqualTo(3 - ddc.AccelerationMax.Linear * 0.1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(p => p.Key.Linear == 3 - ddc.AccelerationMax.Linear * 0.1).Key.Angular, Is.EqualTo(0));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(p => p.Key.Linear == 3 - ddc.AccelerationMax.Linear * 0.1).Value, Is.EqualTo(new Vector2(-1, -1)));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Keys.Max(v => v.Linear), Is.EqualTo(3 + ddc.AccelerationMax.Linear * 0.1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(p => p.Key.Linear == 3 + ddc.AccelerationMax.Linear * 0.1).Key.Angular, Is.EqualTo(0));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(p => p.Key.Linear == 3 + ddc.AccelerationMax.Linear * 0.1).Value, Is.EqualTo(new Vector2(1, 1)));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Keys.Min(v => v.Angular), Is.EqualTo(-ddc.AccelerationMax.Angular * 0.1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(p => p.Key.Angular == -ddc.AccelerationMax.Angular * 0.1).Key.Linear, Is.EqualTo(3));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(p => p.Key.Angular == -ddc.AccelerationMax.Angular * 0.1).Value, Is.EqualTo(new Vector2(1, -1)));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Keys.Max(v => v.Angular), Is.EqualTo(ddc.AccelerationMax.Angular * 0.1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(p => p.Key.Angular == ddc.AccelerationMax.Angular * 0.1).Key.Linear, Is.EqualTo(3));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(p => p.Key.Angular == +ddc.AccelerationMax.Angular * 0.1).Value, Is.EqualTo(new Vector2(-1, 1)));

            Assert.That(ddc.Generate(new Velocity(3, 0))[new Velocity(3, 0)], Is.EqualTo(new Vector2()));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Keys.Count(v => v.Linear == 3), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Keys.Count(v => v.Angular == 0), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Keys.Count(v => Math.Abs(v.Linear - (3 + ddc.VelocityStep.Linear)) < 0.0001), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1 - 2));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Keys.Count(v => Math.Abs(v.Angular - ddc.VelocityStep.Angular) < 0.0001), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1 - 2));
        }
    }
}