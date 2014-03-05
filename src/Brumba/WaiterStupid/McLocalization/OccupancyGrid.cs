using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
	public class OccupancyGrid
	{
		readonly bool[,] _occupancy;
	    readonly float _cellSize;
        readonly Point _size;

		public OccupancyGrid(bool[,] occupancy, float cellSize)
		{
            Contract.Requires(occupancy != null);
            Contract.Requires(cellSize > 0);

			_occupancy = occupancy;
			_cellSize = cellSize;
			_size = new Point(occupancy.GetLength(1), occupancy.GetLength(0));
		}

        public bool this[Point cell]
        {
            get
            {
                Contract.Requires(cell.X >= 0);
                Contract.Requires(cell.X < Size.X);
                Contract.Requires(cell.Y >= 0);
                Contract.Requires(cell.Y < Size.Y);

                return _occupancy[cell.Y, cell.X];
            }
        }

	    public Point Size
	    {
	        get { return _size; }
	    }

	    public float CellSize
	    {
	        get { return _cellSize; }
	    }

	    public Vector2 CellToPos(Point cell)
		{
            Contract.Requires(cell.X >= 0);
            Contract.Requires(cell.X < Size.X);
            Contract.Requires(cell.Y >= 0);
            Contract.Requires(cell.Y < Size.Y);
            Contract.Ensures(Contract.Result<Vector2>().X > 0);
            Contract.Ensures(Contract.Result<Vector2>().X < Size.X * CellSize);
            Contract.Ensures(Contract.Result<Vector2>().Y > 0);
            Contract.Ensures(Contract.Result<Vector2>().Y < Size.Y * CellSize);

			return new Vector2(cell.X + 0.5f, cell.Y + 0.5f) * CellSize;
		}

		public Point PosToCell(Vector2 position)
		{
            Contract.Requires(position.X >= 0);
            Contract.Requires(position.X < Size.X * CellSize);
            Contract.Requires(position.Y >= 0);
            Contract.Requires(position.Y < Size.Y * CellSize);
            Contract.Ensures(Contract.Result<Point>().X >= 0);
            Contract.Ensures(Contract.Result<Point>().X < Size.X);
            Contract.Ensures(Contract.Result<Point>().Y >= 0);
            Contract.Ensures(Contract.Result<Point>().Y < Size.Y);

			return new Point((int)(position.X / CellSize), (int)(position.Y / CellSize));
		}
	}
}