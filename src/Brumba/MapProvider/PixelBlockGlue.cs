using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Xna.Framework;
using Point = Microsoft.Xna.Framework.Point;
using DC = System.Diagnostics.Contracts;

namespace Brumba.MapProvider
{
    [DC.ContractClassAttribute(typeof(IPixelBlockGlueContract))]
    public interface IPixelBlockGlue
    {
        IEnumerable<PixelBlock> GluePixelBlocks(IEnumerable<Point> pixels);
    }

    public class PixelBlockGlue : IPixelBlockGlue
    {
		public IEnumerable<PixelBlock> GluePixelBlocks(IEnumerable<Point> pixels)
		{
			return from psEqualStartX in GluePixelStripes(pixels).GroupBy(p => p.Start.X)
				   from psEqualLength in psEqualStartX.GroupBy(p => p.Length)
				   from boxLeftTopAndHeight in GetSequenceLengthes(psEqualLength, ps => ps.Start.Y)
				   select new PixelBlock(boxLeftTopAndHeight.Item1.Start, new Point(psEqualLength.First().Length, boxLeftTopAndHeight.Item2));
		}

		public IEnumerable<PixelStripe> GluePixelStripes(IEnumerable<Point> pixels)
		{
            DC.Contract.Requires(pixels != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<PixelStripe>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<PixelStripe>>().Sum(ps => ps.Length) == pixels.Count());

			return GetRows(pixels).SelectMany(pixelRow => GetSequenceLengthes(pixelRow, ps => ps.X).
			    Select(stripeStartAndLength => new PixelStripe(stripeStartAndLength.Item1, stripeStartAndLength.Item2)));
		}

        static IEnumerable<IEnumerable<Point>> GetRows(IEnumerable<Point> pixels)
		{
            DC.Contract.Requires(pixels != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<IEnumerable<Point>>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<IEnumerable<Point>>>().Sum(r => r.Count()) == pixels.Count());

			return pixels.ToLookup(p => p.Y, p => p).Select(gr => gr);
		}

		static IEnumerable<Tuple<T, int>> GetSequenceLengthes<T>(IEnumerable<T> points, Func<T, int> getPointPosition)
		{
            DC.Contract.Requires(points != null);
            DC.Contract.Requires(points.Any());
            DC.Contract.Requires(getPointPosition != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Tuple<T, int>>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Tuple<T, int>>>().Any());
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Tuple<T, int>>>().Sum(t => t.Item2) == points.Count());

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
            DC.Contract.Requires(start.ToVec().GreaterOrEqual(new Vector2()));
            DC.Contract.Requires(length > 0);

			Start = start;
			Length = length;
		}
	}

	public class PixelBlock
	{
		public Point LeftTop { get; private set; }
		public Point Size { get; private set; }

		public PixelBlock(Point leftTop, Point size)
		{
            DC.Contract.Requires(leftTop.ToVec().GreaterOrEqual(new Vector2()));
            DC.Contract.Requires(size.ToVec().GreaterOrEqual(new Vector2(1, 1)));

			LeftTop = leftTop;
			Size = size;
		}
	}

    [DC.ContractClassForAttribute(typeof(IPixelBlockGlue))]
    abstract class IPixelBlockGlueContract : IPixelBlockGlue
    {
        public IEnumerable<PixelBlock> GluePixelBlocks(IEnumerable<Point> pixels)
        {
            DC.Contract.Requires(pixels != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<PixelBlock>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<PixelBlock>>().Sum(pb => pb.Size.X * pb.Size.Y) == pixels.Count());

            return default(IEnumerable<PixelBlock>);
        }
    }
}