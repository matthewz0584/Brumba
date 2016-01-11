using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Brumba.Utils;
using Microsoft.Dss.Core.Attributes;
using DC = System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;

namespace Brumba.MapProvider
{
    [DataContract]
	public class OccupancyGrid : IFreezable, IEnumerable<Tuple<Point, bool>>
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

        public bool this[int x, int y]
        {
            get
            {
                DC.Contract.Requires(Freezed);
                DC.Contract.Requires(Covers(new Point(x, y)));

                return _occupancy[y, x];
            }
        }

        public bool this[Point cell]
        {
            get
            {
                DC.Contract.Requires(Freezed);
                DC.Contract.Requires(Covers(cell));

                return this[cell.X, cell.Y];
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
            get { return _cellSize; }
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
	        get
	        {
				var occupancyCopy = new bool[_occupancy.GetLength(0), _occupancy.GetLength(1)];
				Array.Copy(_occupancy, 0, occupancyCopy, 0, _occupancy.Length);
		        return occupancyCopy;
	        }
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

			return new Vector2(cell.X + 0.5f, cell.Y + 0.5f) * CellSize;
		}

		public Point PosToCell(Vector2 position)
		{
            DC.Contract.Requires(Freezed);

			return new Point((int)(position.X / CellSize), (int)(position.Y / CellSize));
		}

	    [Pure]
        public bool Covers(Vector2 position)
	    {
            DC.Contract.Requires(Freezed);

	        return position.BetweenL(new Vector2(), SizeInMeters);
	    }

        [Pure]
        public bool Covers(Point cell)
        {
            DC.Contract.Requires(Freezed);

            return cell.BetweenL(new Point(), SizeInCells);
        }

        public void Freeze()
        {
            Freezed = true;
        }

        public bool Freezed { get; private set; }
        
        public IEnumerator<Tuple<Point, bool>> GetEnumerator()
        {
            for (int y = 0; y < SizeInCells.Y; ++y)
                for (int x = 0; x < SizeInCells.X; ++x)
                    yield return Tuple.Create(new Point(x, y), this[x, y]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}