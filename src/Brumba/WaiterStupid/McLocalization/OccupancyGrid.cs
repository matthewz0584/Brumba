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
			_occupancy = occupancy;
			_cellSize = cellSize;
			_size = new Point(occupancy.GetLength(1), occupancy.GetLength(0));
		}

        public bool this[Point p]
        {
            get { return _occupancy[p.Y, p.X]; }
        }

	    public Point Size
	    {
	        get { return _size; }
	    }

	    public Vector2 CellToPos(Point point)
		{
			return new Vector2(point.X + 0.5f, point.Y + 0.5f) * _cellSize;
		}

		public Point PosToCell(Vector2 position)
		{
			return new Point((int)(position.X / _cellSize), (int)(position.Y / _cellSize));
		}
	}
}