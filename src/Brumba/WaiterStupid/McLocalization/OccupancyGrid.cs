using System.Linq;
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

		public float DistanceToObstacle(Vector2 position)
		{
			return (PointToPos(FindNearestOccupied(PosToPoint(position))) - position).Length();
		}

		Point FindNearestOccupied(Point point)
		{
			return new SquareFringeGenerator(_size).Generate(point).First(p => _occupancy[p.Y, p.X]);
		}

		Vector2 PointToPos(Point point)
		{
			return new Vector2(point.X + 0.5f, point.Y + 0.5f) * _cellSize;
		}

		Point PosToPoint(Vector2 position)
		{
			return new Point((int)(position.X / _cellSize), (int)(position.Y / _cellSize));
		}
	}
}