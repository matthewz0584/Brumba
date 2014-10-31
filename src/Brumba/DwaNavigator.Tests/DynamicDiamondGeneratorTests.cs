using System;
using System.Collections.Generic;
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
            var ddc = new DynamicDiamondGenerator(1d, 1.5d, 3d, 0.1);
            var dd = ddc.Generate(new Velocity(3, 0));

            Assert.That(dd.GetLength(0) * dd.GetLength(1), Is.EqualTo((2 * DynamicDiamondGenerator.STEPS_NUMBER + 1) * (2 * DynamicDiamondGenerator.STEPS_NUMBER + 1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Count(), Is.EqualTo((2 * DynamicDiamondGenerator.STEPS_NUMBER + 1) * (2 * DynamicDiamondGenerator.STEPS_NUMBER + 1)));
            Assert.That(dd.Cast<VelocityAcceleration>().Min(v => v.Velocity.Linear), Is.EqualTo(3 - ddc.AccelerationMax.Linear * 0.1));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 - ddc.AccelerationMax.Linear * 0.1).Velocity.Angular, Is.EqualTo(0));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 - ddc.AccelerationMax.Linear * 0.1).WheelAcceleration, Is.EqualTo(new Vector2(-1, -1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Max(v => v.Velocity.Linear), Is.EqualTo(3 + ddc.AccelerationMax.Linear * 0.1));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 + ddc.AccelerationMax.Linear * 0.1).Velocity.Angular, Is.EqualTo(0));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 + ddc.AccelerationMax.Linear * 0.1).WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Min(v => v.Velocity.Angular), Is.EqualTo(-ddc.AccelerationMax.Angular * 0.1));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Angular == -ddc.AccelerationMax.Angular * 0.1).Velocity.Linear, Is.EqualTo(3));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Angular == -ddc.AccelerationMax.Angular * 0.1).WheelAcceleration, Is.EqualTo(new Vector2(1, -1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Max(v => v.Velocity.Angular), Is.EqualTo(ddc.AccelerationMax.Angular * 0.1));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Angular == ddc.AccelerationMax.Angular * 0.1).Velocity.Linear, Is.EqualTo(3));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Angular == +ddc.AccelerationMax.Angular * 0.1).WheelAcceleration, Is.EqualTo(new Vector2(-1, 1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 && p.Velocity.Angular == 0).WheelAcceleration, Is.EqualTo(new Vector2()));

            Assert.That(dd.Cast<VelocityAcceleration>().Count(v => v.Velocity.Linear == 3), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1));
            Assert.That(dd.Cast<VelocityAcceleration>().Count(v => v.Velocity.Angular == 0), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1));

            Assert.That(dd.Cast<VelocityAcceleration>().Count(v => Math.Abs(v.Velocity.Linear - (3 + ddc.VelocityStep.Linear)) < 0.0001), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1 - 2));
            Assert.That(dd.Cast<VelocityAcceleration>().Count(v => Math.Abs(v.Velocity.Angular - ddc.VelocityStep.Angular) < 0.0001), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1 - 2));
        }
    }
}