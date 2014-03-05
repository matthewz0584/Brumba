using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
	public class GridSquareFringeGenerator
	{
		private readonly Point _gridSize;

		public GridSquareFringeGenerator(Point gridSize)
		{
            Contract.Requires(gridSize.X > 0);
            Contract.Requires(gridSize.Y >=0);

			_gridSize = gridSize;
		}

	    public Point GridSize
	    {
	        get { return _gridSize; }
	    }

	    public IEnumerable<Point> Generate(Point center)
		{
            Contract.Requires(center.X >= 0);
            Contract.Requires(center.X < GridSize.X);
            Contract.Requires(center.Y >= 0);
            Contract.Requires(center.Y < GridSize.Y);

			yield return new Point(center.X, center.Y);
			var curBorderHalfLength = 1;
			while (true)
			{
				if (IsGridInterior(center.Y + curBorderHalfLength, GridSize.Y))
					foreach (var p in EnumerateCoords(center.X, curBorderHalfLength, GridSize.X).Select(x => new Point(x, center.Y + curBorderHalfLength)))
						yield return p;

				if (IsGridInterior(center.Y - curBorderHalfLength, GridSize.Y))
					foreach (var p in EnumerateCoords(center.X, curBorderHalfLength, GridSize.X).Select(x => new Point(x, center.Y - curBorderHalfLength)))
						yield return p;

				if (IsGridInterior(center.X + curBorderHalfLength, GridSize.X))
					foreach (var p in EnumerateCoords(center.Y, curBorderHalfLength - 1, GridSize.Y).Select(y => new Point(center.X + curBorderHalfLength, y)))
						yield return p;

				if (IsGridInterior(center.X - curBorderHalfLength, GridSize.X))
					foreach (var p in EnumerateCoords(center.Y, curBorderHalfLength - 1, GridSize.Y).Select(y => new Point(center.X - curBorderHalfLength, y)))
						yield return p;

				++curBorderHalfLength;
			}
		}

		static IEnumerable<int> EnumerateCoords(int around, int radius, int gridSize)
		{
            Contract.Requires(around >= 0);
            Contract.Requires(around < gridSize);
            Contract.Requires(radius >= 0);
            Contract.Requires(radius < gridSize);
            Contract.Requires(gridSize > 0);

			return Enumerable.Range(around - radius, radius * 2 + 1).Where(c => IsGridInterior(c, gridSize));
		}

		static bool IsGridInterior(int coord, int gridSize)
		{
            Contract.Requires(gridSize > 0);

			return coord < gridSize && coord >= 0;
		}		
	}
}