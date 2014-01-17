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

		[Test]
		public void DoubleEqualsWithin()
		{
			Assert.That(MathHelper2.EqualsWithin(10, 10, 0.1));
			Assert.That(MathHelper2.EqualsWithin(10, 11, 0.1));
			Assert.That(MathHelper2.EqualsWithin(10, 9, 0.1));

			Assert.That(MathHelper2.EqualsWithin(-10, -10, 0.1));
			Assert.That(MathHelper2.EqualsWithin(-10, -11, 0.1));
			Assert.That(MathHelper2.EqualsWithin(-10, -9, 0.1));
			
			Assert.That(MathHelper2.EqualsWithin(10, 12, 0.1), Is.False);
			Assert.That(MathHelper2.EqualsWithin(10, 8, 0.1), Is.False);
		}

	    [Test]
	    public void VectorEqualWithin()
	    {
		    Assert.That(new Vector2(10, 0).EqualsWithin(new Vector2(9, 0), 0.1));
			Assert.That(new Vector2(10, 0).EqualsWithin(new Vector2(8, 0), 0.1), Is.False);

			Assert.That(new Vector2(3, 4).EqualsWithin(new Vector2(3.0f, 4.0f * 1.125f), 0.1));
			Assert.That(new Vector2(3, 4).EqualsWithin(new Vector2(3.0f, 4.0f * 1.126f), 0.1), Is.False);
	    }
    }
}
