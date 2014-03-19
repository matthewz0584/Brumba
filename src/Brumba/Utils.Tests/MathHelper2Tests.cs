using System;
using Microsoft.Xna.Framework;
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
            Assert.That(MathHelper2.ToPositiveAngle(float.NaN), Is.EqualTo(float.NaN));
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

            Assert.That(MathHelper2.AngleDifference(float.NaN, float.NaN), Is.EqualTo(float.NaN));
        }

		[Test]
		public void DoubleEqualsRelatively()
		{
			Assert.That(10f.EqualsRelatively(10, 0.1f));
            Assert.That(11f.EqualsRelatively(10, 0.1f));
			Assert.That(12f.EqualsRelatively(10, 0.1f), Is.False);
			Assert.That(9f.EqualsRelatively(10, 0.1f));
            Assert.That(8f.EqualsRelatively(10, 0.1f), Is.False);

			Assert.That((-10f).EqualsRelatively(-10, 0.1f));            
			Assert.That((-11f).EqualsRelatively(-10, 0.1f));
            Assert.That((-12f).EqualsRelatively(-10, 0.1f), Is.False);
			Assert.That((-9f).EqualsRelatively(-10, 0.1f));
            Assert.That((-8f).EqualsRelatively(-10, 0.1f), Is.False);

            Assert.That(10f.EqualsRelatively(10, 0));
            Assert.That(10.000001f.EqualsRelatively(10, 0), Is.False);
		}

	    [Test]
	    public void VectorEqualsRelatively()
	    {
            Assert.That(new Vector2(10, 0).EqualsRelatively(new Vector2(11, 0), 0.1));
            Assert.That(new Vector2(10, 0).EqualsRelatively(new Vector2(12, 0), 0.1), Is.False);
		    Assert.That(new Vector2(10, 0).EqualsRelatively(new Vector2(9, 0), 0.1));
			Assert.That(new Vector2(10, 0).EqualsRelatively(new Vector2(8, 0), 0.1), Is.False);

			Assert.That(new Vector2(3, 4).EqualsRelatively(new Vector2(3.0f, 4.0f * 1.125f), 0.1));
			Assert.That(new Vector2(3, 4).EqualsRelatively(new Vector2(3.0f, 4.0f * 1.126f), 0.1), Is.False);
	    }

	    [Test]
	    public void ToMinAbsValueAngle()
	    {
            Assert.That(0f.ToMinAbsValueAngle(), Is.EqualTo(0));
            Assert.That((-MathHelper.Pi).ToMinAbsValueAngle(), Is.EqualTo(-MathHelper.Pi));
            Assert.That(MathHelper.Pi.ToMinAbsValueAngle(), Is.EqualTo(-MathHelper.Pi));
            Assert.That(MathHelper.PiOver4.ToMinAbsValueAngle(), Is.EqualTo(MathHelper.PiOver4));
            Assert.That((3 * MathHelper.PiOver4).ToMinAbsValueAngle(), Is.EqualTo(3 * MathHelper.PiOver4));
            Assert.That((5 * MathHelper.PiOver4).ToMinAbsValueAngle(), Is.EqualTo(-3 * MathHelper.PiOver4));
            Assert.That((-3 * MathHelper.PiOver4).ToMinAbsValueAngle(), Is.EqualTo(-3 * MathHelper.PiOver4));
            Assert.That((-5 * MathHelper.PiOver4).ToMinAbsValueAngle(), Is.EqualTo(3 * MathHelper.PiOver4));
            Assert.That((5 * MathHelper.PiOver4 + MathHelper.TwoPi).ToMinAbsValueAngle(), Is.EqualTo(-3 * MathHelper.PiOver4));
	    }

        [Test]
        public void AngleMean()
        {
            Assert.That(MathHelper2.AngleMean(new[] { MathHelper.PiOver2, MathHelper.PiOver4 }), Is.EqualTo(MathHelper.Pi * 3 / 8));
            Assert.That(MathHelper2.AngleMean(new[] { 3 * MathHelper.PiOver4, 5 * MathHelper.PiOver4 }), Is.EqualTo(MathHelper.Pi).Within(1e-5));
            Assert.That(MathHelper2.AngleMean(new[] { MathHelper.PiOver4, 7 * MathHelper.PiOver4 }), Is.EqualTo(0).Within(1e-5));
            Assert.That(MathHelper2.AngleMean(new[] { MathHelper.PiOver4, 5 * MathHelper.PiOver4 }), Is.NaN);
        }
    }
}
