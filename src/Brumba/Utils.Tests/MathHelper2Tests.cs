using System;
using NUnit.Framework;

namespace Brumba.Utils.Tests
{
    [TestFixture]
    public class MathHelper2Tests
    {
        [Test]
        public void ToPositiveAngle()
        {
            Assert.That(MathHelper2.ToPositiveAngle((float)Math.PI / 4), Is.EqualTo((float)Math.PI / 4));
            Assert.That(MathHelper2.ToPositiveAngle(-(float)Math.PI / 4), Is.EqualTo((float)Math.PI * 7 / 4));
            Assert.That(MathHelper2.ToPositiveAngle(-(float)Math.PI), Is.EqualTo((float)Math.PI));
            Assert.That(MathHelper2.ToPositiveAngle(-6 * (float)Math.PI), Is.EqualTo(0).Within(1e-6));
        }

        [Test]
        public void AngleDifference()
        {
            Assert.That(MathHelper2.AngleDifference((float)Math.PI / 2, (float)Math.PI / 4), Is.EqualTo((float)Math.PI / 4));
            Assert.That(MathHelper2.AngleDifference((float)Math.PI / 4, (float)Math.PI / 2), Is.EqualTo((float)Math.PI / 4));
            
            Assert.That(MathHelper2.AngleDifference((float)Math.PI / 4, -(float)Math.PI / 4), Is.EqualTo((float)Math.PI / 2).Within(1e-5));
            Assert.That(MathHelper2.AngleDifference(-(float)Math.PI / 4, (float)Math.PI / 4), Is.EqualTo((float)Math.PI / 2).Within(1e-5));

            Assert.That(MathHelper2.AngleDifference((float)Math.PI * 7 / 4, (float)Math.PI / 4), Is.EqualTo((float)Math.PI / 2).Within(1e-5));
            Assert.That(MathHelper2.AngleDifference((float)Math.PI / 4, (float)Math.PI * 7 / 4), Is.EqualTo((float)Math.PI / 2).Within(1e-5));
        }
    }
}
