using System;
using System.Linq;
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

            Assert.That(ddc.Generate(new Velocity(3, 0)).Min(v => v.Linear), Is.EqualTo(3 - ddc.AccelerationMax.Linear * 0.1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(v => v.Linear == 3 - ddc.AccelerationMax.Linear * 0.1).Angular, Is.EqualTo(0));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Max(v => v.Linear), Is.EqualTo(3 + ddc.AccelerationMax.Linear * 0.1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(v => v.Linear == 3 + ddc.AccelerationMax.Linear * 0.1).Angular, Is.EqualTo(0));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Min(v => v.Angular), Is.EqualTo(-ddc.AccelerationMax.Angular * 0.1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(v => v.Angular == -ddc.AccelerationMax.Angular * 0.1).Linear, Is.EqualTo(3));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Max(v => v.Angular), Is.EqualTo(ddc.AccelerationMax.Angular * 0.1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Single(v => v.Angular == ddc.AccelerationMax.Angular * 0.1).Linear, Is.EqualTo(3));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Contains(new Velocity(3, 0)));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Count(v => v.Linear == 3), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Count(v => v.Angular == 0), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1));

            Assert.That(ddc.Generate(new Velocity(3, 0)).Count(v => v.Linear == 3 + ddc.VelocityStep.Linear), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1 - 2));
            Assert.That(ddc.Generate(new Velocity(3, 0)).Count(v => Math.Abs(v.Angular - ddc.VelocityStep.Angular) < 0.0001), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1 - 2));
        }

        //[Test]
        //public void GetDynamicDiamondLinearIsPositive()
        //{
        //    var ddc = new DynamicDiamondGenerator(0.5d, 1.5d, 3d, 0.1);

        //    Assert.That(ddc.Generate(new Velocity(0, 0)).Count(),
        //        Is.EqualTo((2 * DynamicDiamondGenerator.STEPS_NUMBER + 1) * (2 * DynamicDiamondGenerator.STEPS_NUMBER + 1) / 2.0 + (2 * DynamicDiamondGenerator.STEPS_NUMBER + 1) / 2.0 ));

        //    Assert.That(ddc.Generate(new Velocity(0, 1)).Min(v => v.Linear), Is.EqualTo(0));

        //    Assert.That(ddc.Generate(new Velocity(0, 1)).Max(v => v.Linear), Is.EqualTo(ddc.AccelerationMax.Linear * 0.1));
        //    Assert.That(ddc.Generate(new Velocity(0, 1)).Single(v => v.Linear == ddc.AccelerationMax.Linear * 0.1).Angular, Is.EqualTo(1));

        //    Assert.That(ddc.Generate(new Velocity(0, 1)).Min(v => v.Angular), Is.EqualTo(1 - ddc.AccelerationMax.Angular * 0.1));
        //    Assert.That(ddc.Generate(new Velocity(0, 1)).Single(v => v.Angular == 1 - ddc.AccelerationMax.Angular * 0.1).Linear, Is.EqualTo(0));

        //    Assert.That(ddc.Generate(new Velocity(0, 1)).Max(v => v.Angular), Is.EqualTo(1 + ddc.AccelerationMax.Angular * 0.1));
        //    Assert.That(ddc.Generate(new Velocity(0, 1)).Single(v => v.Angular == 1 + ddc.AccelerationMax.Angular * 0.1).Linear, Is.EqualTo(0));

        //    Assert.That(ddc.Generate(new Velocity(0, 1)).Count(v => v.Linear == 0), Is.EqualTo(2 * DynamicDiamondGenerator.STEPS_NUMBER + 1));
        //    Assert.That(ddc.Generate(new Velocity(0, 1)).Count(v => v.Angular == 1), Is.EqualTo(DynamicDiamondGenerator.STEPS_NUMBER + 1));
        //}
    }
}