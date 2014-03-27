using System.Diagnostics.Contracts;
using Brumba.Utils;
using Microsoft.Dss.Core.Attributes;
using DC = System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    [DataContract]
	public class OccupancyGrid : IFreezable
	{
		bool[,] _occupancy;
	    float _cellSize;

        public OccupancyGrid()
        {}

		public OccupancyGrid(bool[,] occupancy, float cellSize)
		{
            DC.Contract.Requires(occupancy != null);
            DC.Contract.Requires(occupancy.GetLength(1) > 0);
            DC.Contract.Requires(occupancy.GetLength(0) > 0);
            DC.Contract.Requires(cellSize > 0);
            DC.Contract.Ensures(CellSize == cellSize);

			_occupancy = occupancy;
			_cellSize = cellSize;
		    
            Freeze();
		}

        public bool this[Point cell]
        {
            get
            {
                DC.Contract.Requires(Freezed);
                DC.Contract.Requires(Covers(cell));

                return _occupancy[cell.Y, cell.X];
            }
        }

        public bool this[Vector2 position]
        {
            get
            {
                DC.Contract.Requires(Freezed);
                DC.Contract.Requires(Covers(position));

                return this[PosToCell(position)];
            }
        }

	    public Point SizeInCells
	    {
	        get
	        {
	            DC.Contract.Requires(Freezed);
                
                return new Point(_occupancy.GetLength(1), _occupancy.GetLength(0));
	        }
	    }

        public Vector2 SizeInMeters
        {
            get
            {
                DC.Contract.Requires(Freezed);
                
                return new Vector2(SizeInCells.X * CellSize, SizeInCells.Y * CellSize);
            }
        }

        [DataMember]
	    public float CellSize
	    {
            get { DC.Contract.Requires(Freezed); return _cellSize; }
            set
            {
                DC.Contract.Requires(!Freezed);
                DC.Contract.Requires(value > 0);
                DC.Contract.Ensures(CellSize == value);
                
                _cellSize = value;
            }
	    }

        [DataMember]
        public bool[,] Data
        {
            get { DC.Contract.Requires(!Freezed); return _occupancy; }
            set
            {
                DC.Contract.Requires(!Freezed);
                DC.Contract.Requires(value != null);
                DC.Contract.Requires(value.GetLength(1) > 0);
                DC.Contract.Requires(value.GetLength(0) > 0);

                _occupancy = value;
            }
        }

	    public Vector2 CellToPos(Point cell)
		{
            DC.Contract.Requires(Freezed);
            DC.Contract.Requires(Covers(cell));
            DC.Contract.Ensures(Covers(DC.Contract.Result<Vector2>()));

			return new Vector2(cell.X + 0.5f, cell.Y + 0.5f) * CellSize;
		}

		public Point PosToCell(Vector2 position)
		{
            DC.Contract.Requires(Freezed);
            DC.Contract.Requires(Covers(position));
            DC.Contract.Ensures(Covers(DC.Contract.Result<Point>()));

			return new Point((int)(position.X / CellSize), (int)(position.Y / CellSize));
		}

	    [Pure]
        public bool Covers(Vector2 position)
	    {
            DC.Contract.Requires(Freezed);

	        return position.Between(new Vector2(), SizeInMeters);
	    }

        [Pure]
        public bool Covers(Point cell)
        {
            DC.Contract.Requires(Freezed);

            return cell.Between(new Point(), SizeInCells);
        }

        public void Freeze()
        {
            Freezed = true;
        }

        public bool Freezed { get; private set; }
	}
}