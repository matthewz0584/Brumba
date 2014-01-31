using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Brumba.FloorPlanBuilder
{
	public class PixelGlue
	{
		public IEnumerable<PixelBlock> GetPixelBlocks(IEnumerable<Point> pixels)
		{
			return from psEqualStartX in GetPixelStripes(pixels).GroupBy(p => p.Start.X)
				   from psEqualLength in psEqualStartX.GroupBy(p => p.Length)
				   from boxLeftTopAndHeight in GetSequenceLengthes(psEqualLength, ps => ps.Start.Y)
				   select new PixelBlock(boxLeftTopAndHeight.Item1.Start, psEqualLength.First().Length, boxLeftTopAndHeight.Item2);
		}

		public IEnumerable<PixelStripe> GetPixelStripes(IEnumerable<Point> pixels)
		{
			return GetRows(pixels).SelectMany(GetPixelStripeFromRow);
		}

		static IEnumerable<PixelStripe> GetPixelStripeFromRow(IEnumerable<Point> pixelRow)
		{
			return GetSequenceLengthes(pixelRow, ps => ps.X).
				Select(stripeStartAndLength => new PixelStripe(stripeStartAndLength.Item1, stripeStartAndLength.Item2));
		}

		static IEnumerable<IEnumerable<Point>> GetRows(IEnumerable<Point> pixels)
		{
			return pixels.ToLookup(p => p.Y, p => p).Select(gr => gr);
		}

		static IEnumerable<Tuple<T, int>> GetSequenceLengthes<T>(IEnumerable<T> points, Func<T, int> getPointPosition)
		{
			var curSequenceLength = 1;
			var sortedPoints = points.OrderBy(getPointPosition);
			var curStartPoint = sortedPoints.First();
			foreach (var point in sortedPoints.Skip(1))
			{
				if (getPointPosition(point) == getPointPosition(curStartPoint) + curSequenceLength)
					++curSequenceLength;
				else
				{
					yield return new Tuple<T, int>(curStartPoint, curSequenceLength);
					curSequenceLength = 1;
					curStartPoint = point;
				}
			}
			yield return new Tuple<T, int>(curStartPoint, curSequenceLength);
		}
	}

	public class PixelStripe
	{
		public Point Start { get; private set; }
		public int Length { get; private set; }

		public PixelStripe(Point start, int length)
		{
			Start = start;
			Length = length;
		}
	}

	public class PixelBlock
	{
		public Point LeftTop { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		public PixelBlock(Point leftTop, int width, int height)
		{
			LeftTop = leftTop;
			Width = width;
			Height = height;
		}
	}
}