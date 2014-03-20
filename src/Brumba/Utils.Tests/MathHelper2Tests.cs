using System;
using MathNet.Numerics;
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
            Assert.That(Constants.PiOver4.ToPositiveAngle(), Is.EqualTo(Constants.PiOver4));
            Assert.That((-Constants.PiOver4).ToPositiveAngle(), Is.EqualTo(7 * Constants.PiOver4));
            Assert.That((-Constants.Pi).ToPositiveAngle(), Is.EqualTo(Constants.Pi));
            Assert.That((-6 * Constants.Pi).ToPositiveAngle(), Is.EqualTo(0).Within(1e-6));
            Assert.That(double.NaN.ToPositiveAngle(), Is.NaN);
            Assert.That((-double.Epsilon).ToPositiveAngle(), Is.EqualTo(0));
            Assert.That((-100 * double.Epsilon).ToPositiveAngle(), Is.Not.EqualTo(0));
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
			Assert.That(10d.EqualsRelatively(10, 0.1));
            Assert.That(11d.EqualsRelatively(10, 0.1));
			Assert.That(12d.EqualsRelatively(10, 0.1), Is.False);
			Assert.That(9d.EqualsRelatively(10, 0.1));
            Assert.That(8d.EqualsRelatively(10, 0.1), Is.False);

			Assert.That((-10d).EqualsRelatively(-10, 0.1));
			Assert.That((-11d).EqualsRelatively(-10, 0.1));
            Assert.That((-12d).EqualsRelatively(-10, 0.1), Is.False);
			Assert.That((-9d).EqualsRelatively(-10, 0.1));
            Assert.That((-8d).EqualsRelatively(-10, 0.1), Is.False);

            Assert.That(10d.EqualsRelatively(10, 0));
            Assert.That(10.000001d.EqualsRelatively(10, 0), Is.False);
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
            Assert.That(0d.ToMinAbsValueAngle(), Is.EqualTo(0));
            Assert.That((-Constants.Pi).ToMinAbsValueAngle(), Is.EqualTo(-Constants.Pi));
            Assert.That(Constants.Pi.ToMinAbsValueAngle(), Is.EqualTo(-Constants.Pi));
            Assert.That(Constants.PiOver4.ToMinAbsValueAngle(), Is.EqualTo(Constants.PiOver4));
            Assert.That((3 * Constants.PiOver4).ToMinAbsValueAngle(), Is.EqualTo(3 * Constants.PiOver4));
            Assert.That((5 * Constants.PiOver4).ToMinAbsValueAngle(), Is.EqualTo(-3 * Constants.PiOver4));
            Assert.That((-3 * Constants.PiOver4).ToMinAbsValueAngle(), Is.EqualTo(-3 * Constants.PiOver4));
            Assert.That((-5 * Constants.PiOver4).ToMinAbsValueAngle(), Is.EqualTo(3 * Constants.PiOver4));
            Assert.That((5 * Constants.PiOver4 + Constants.Pi2).ToMinAbsValueAngle(), Is.EqualTo(-3 * Constants.PiOver4).Within(1e-10));
	    }

        [Test]
        public void AngleMean()
        {
            Assert.That(MathHelper2.AngleMean(new[] { Constants.PiOver2, Constants.PiOver4 }), Is.EqualTo(Constants.Pi * 3 / 8));
            Assert.That(MathHelper2.AngleMean(new[] { 3 * Constants.PiOver4, 5 * Constants.PiOver4 }), Is.EqualTo(Constants.Pi));
            Assert.That(MathHelper2.AngleMean(new[] { Constants.PiOver4, 7 * Constants.PiOver4 }), Is.EqualTo(0).Within(1e-5));
            Assert.That(MathHelper2.AngleMean(new[] { Constants.PiOver4, 5 * Constants.PiOver4 }), Is.NaN);
        }
    }
}
