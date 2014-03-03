using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
	public class GridSquareFringeGenerator
	{
		private Point _gridSize;

		public GridSquareFringeGenerator(Point gridSize)
		{
			_gridSize = gridSize;
		}

		public IEnumerable<Point> Generate(Point center)
		{
			yield return new Point(center.X, center.Y);
			var curBorderHalfLength = 1;
			while (true)
			{
				if (IsGridInterior(center.Y + curBorderHalfLength, _gridSize.Y))
					foreach (var p in EnumerateCoords(center.X, curBorderHalfLength, _gridSize.X).Select(x => new Point(x, center.Y + curBorderHalfLength)))
						yield return p;

				if (IsGridInterior(center.Y - curBorderHalfLength, _gridSize.Y))
					foreach (var p in EnumerateCoords(center.X, curBorderHalfLength, _gridSize.X).Select(x => new Point(x, center.Y - curBorderHalfLength)))
						yield return p;

				if (IsGridInterior(center.X + curBorderHalfLength, _gridSize.X))
					foreach (var p in EnumerateCoords(center.Y, curBorderHalfLength - 1, _gridSize.Y).Select(y => new Point(center.X + curBorderHalfLength, y)))
						yield return p;

				if (IsGridInterior(center.X - curBorderHalfLength, _gridSize.X))
					foreach (var p in EnumerateCoords(center.Y, curBorderHalfLength - 1, _gridSize.Y).Select(y => new Point(center.X - curBorderHalfLength, y)))
						yield return p;

				++curBorderHalfLength;
			}
		}

		static IEnumerable<int> EnumerateCoords(int around, int radius, int gridSize)
		{
			return Enumerable.Range(around - radius, radius * 2 + 1).Where(c => IsGridInterior(c, gridSize));
		}

		static bool IsGridInterior(int coord, int gridSize)
		{
			return coord < gridSize && coord >= 0;
		}		
	}
}