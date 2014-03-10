using System.Diagnostics.Contracts;
using Brumba.Utils;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
	public class OccupancyGrid
	{
		readonly bool[,] _occupancy;
	    readonly float _cellSize;
        readonly Point _sizeInCells;

		public OccupancyGrid(bool[,] occupancy, float cellSize)
		{
            Contract.Requires(occupancy != null);
            Contract.Requires(occupancy.GetLength(1) > 0);
            Contract.Requires(occupancy.GetLength(0) > 0);
            Contract.Requires(cellSize > 0);
            Contract.Ensures(SizeInCells.X == occupancy.GetLength(1));
            Contract.Ensures(SizeInCells.Y == occupancy.GetLength(0));
            Contract.Ensures(CellSize == cellSize);

			_occupancy = occupancy;
			_cellSize = cellSize;
			_sizeInCells = new Point(occupancy.GetLength(1), occupancy.GetLength(0));
		}

        public bool this[Point cell]
        {
            get
            {
                Contract.Requires(Covers(cell));

                return _occupancy[cell.Y, cell.X];
            }
        }

        public bool this[Vector2 position]
        {
            get
            {
                Contract.Requires(Covers(position));

                return this[PosToCell(position)];
            }
        }

	    public Point SizeInCells
	    {
	        get { return _sizeInCells; }
	    }

        public Vector2 SizeInMeters
        {
            get { return new Vector2(SizeInCells.X * CellSize, SizeInCells.Y * CellSize); }
        }

	    public float CellSize
	    {
	        get { return _cellSize; }
	    }

	    public Vector2 CellToPos(Point cell)
		{
            Contract.Requires(Covers(cell));
            Contract.Ensures(Covers(Contract.Result<Vector2>()));

			return new Vector2(cell.X + 0.5f, cell.Y + 0.5f) * CellSize;
		}

		public Point PosToCell(Vector2 position)
		{
            Contract.Requires(Covers(position));
            Contract.Ensures(Covers(Contract.Result<Point>()));

			return new Point((int)(position.X / CellSize), (int)(position.Y / CellSize));
		}

	    [Pure]
        public bool Covers(Vector2 position)
	    {
	        return position.Between(new Vector2(), SizeInMeters);
	    }

        [Pure]
        public bool Covers(Point cell)
        {
            return cell.Between(new Point(), SizeInCells);
        }
	}
}