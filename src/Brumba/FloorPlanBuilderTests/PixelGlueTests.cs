using System.Linq;
using Brumba.FloorPlanBuilder;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.FloorPlanBuilderTests
{
	[TestFixture]
	public class PixelGlueTests
	{
		private PixelGlue m_pixelGlue;

		[SetUp]
		public void SetUp()
		{
			m_pixelGlue = new PixelGlue();
		}

		[Test]
		public void OneStripe()
		{
			var pixels = new[] { new Point(1, 1), new Point(2, 1) };

			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Count(), Is.EqualTo(1));
			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Single(p => p.Start == new Point(1, 1)).Length, Is.EqualTo(2));
		}

		[Test]
		public void TwoStripesInOneRow()
		{
			var pixels = new[] { new Point(3, 1), new Point(1, 1), new Point(2, 1), new Point(5, 1) };

			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Count(), Is.EqualTo(2));
			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Single(p => p.Start == new Point(1, 1)).Length, Is.EqualTo(3));
			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Single(p => p.Start == new Point(5, 1)).Length, Is.EqualTo(1));
		}

		[Test]
		public void TwoStripesInTwoRows()
		{
			var pixels = new[] { new Point(1, 1), new Point(2, 1), new Point(3, 2), new Point(4, 2) };

			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Count(), Is.EqualTo(2));
			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Single(p => p.Start == new Point(1, 1)).Length, Is.EqualTo(2));
			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Single(p => p.Start == new Point(3, 2)).Length, Is.EqualTo(2));
		}

		[Test]
		public void OneStripeOnePixel()
		{
			var pixels = new[] { new Point(1, 1) };

			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Count(), Is.EqualTo(1));
			Assert.That(m_pixelGlue.GetPixelStripes(pixels).Single(p => p.Start == new Point(1, 1)).Length, Is.EqualTo(1));
		}

		[Test]
		public void ZeroStripesZeroPixels()
		{
			Assert.That(m_pixelGlue.GetPixelStripes(new Point[] { }).Count(), Is.EqualTo(0));
		}

		[Test]
		public void OneBlockOnePixel()
		{
			var pixels = new[] { new Point(1, 1) };

			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).Count(), Is.EqualTo(1));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).Single().LeftTop, Is.EqualTo(new Point(1, 1)));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).Single().Width, Is.EqualTo(1));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).Single().Height, Is.EqualTo(1));
		}

		[Test]
		public void OneBlockTwoStripe()
		{
			var pixels = new[] { new Point(1, 1), new Point(1, 2), new Point(1, 3), new Point(2, 1), new Point(2, 2), new Point(2, 3) };

			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).Count(), Is.EqualTo(1));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).Single().LeftTop, Is.EqualTo(new Point(1, 1)));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).Single().Width, Is.EqualTo(2));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).Single().Height, Is.EqualTo(3));
		}

		[Test]
		public void HorseMove()
		{
			var pixels = new[] { new Point(1, 1), new Point(2, 1), new Point(1, 2), new Point(1, 3) };

			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).Count(), Is.EqualTo(2));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).First(p => p.Width == 2).LeftTop, Is.EqualTo(new Point(1, 1)));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).First(p => p.Width == 2).Height, Is.EqualTo(1));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).First(p => p.Width == 1).LeftTop, Is.EqualTo(new Point(1, 2)));
			Assert.That(m_pixelGlue.GetPixelBlocks(pixels).First(p => p.Width == 1).Height, Is.EqualTo(2));
		}
	}
}