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
                Contract.Requires(cell.Between(new Point(), SizeInCells));

                return _occupancy[cell.Y, cell.X];
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
            Contract.Requires(cell.Between(new Point(), SizeInCells));
            Contract.Ensures(Contract.Result<Vector2>().Between(new Vector2(), SizeInMeters));

			return new Vector2(cell.X + 0.5f, cell.Y + 0.5f) * CellSize;
		}

		public Point PosToCell(Vector2 position)
		{
            Contract.Requires(position.Between(new Vector2(), SizeInMeters));
            Contract.Ensures(Contract.Result<Point>().Between(new Point(), SizeInCells));

			return new Point((int)(position.X / CellSize), (int)(position.Y / CellSize));
		}
	}
}