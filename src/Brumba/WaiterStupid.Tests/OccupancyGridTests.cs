using Brumba.WaiterStupid.McLocalization;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
	[TestFixture]
	public class OccupancyGridTests
	{
	    private OccupancyGrid _grid;

	    [SetUp]
	    public void SetUp()
	    {
	        _grid = new OccupancyGrid(new[,] { { false, true, false }, { false, false, true } }, 0.2f);
	    }

	    [Test]
	    public void SizeInCells()
	    {
	        Assert.That(_grid.SizeInCells, Is.EqualTo(new Point(3, 2)));
	    }

        [Test]
        public void SizeInMeters()
        {
            Assert.That(_grid.SizeInMeters, Is.EqualTo(new Vector2(3 * 0.2f, 2 * 0.2f)));
        }

	    [Test]
	    public void CellIndexer()
	    {
            Assert.That(_grid[new Point(0, 0)], Is.False);
            Assert.That(_grid[new Point(1, 0)], Is.True);
            Assert.That(_grid[new Point(2, 0)], Is.False);

            Assert.That(_grid[new Point(0, 1)], Is.False);
            Assert.That(_grid[new Point(1, 1)], Is.False);
            Assert.That(_grid[new Point(2, 1)], Is.True);
	    }

        [Test]
        public void PositionIndexer()
        {
            Assert.That(_grid[new Vector2(0.1f, 0.1f)], Is.False);
            Assert.That(_grid[new Vector2(0.3f, 0.1f)], Is.True);
        }

	    [Test]
	    public void PosToCell()
	    {
            Assert.That(_grid.PosToCell(new Vector2(0.1f, 0)), Is.EqualTo(new Point(0, 0)));
            Assert.That(_grid.PosToCell(new Vector2(0, 0)), Is.EqualTo(new Point(0, 0)));
            Assert.That(_grid.PosToCell(new Vector2(0.19f, 0)), Is.EqualTo(new Point(0, 0)));

            Assert.That(_grid.PosToCell(new Vector2(0, 0.3f)), Is.EqualTo(new Point(0, 1)));
            Assert.That(_grid.PosToCell(new Vector2(0, 0.2f)), Is.EqualTo(new Point(0, 1)));
            Assert.That(_grid.PosToCell(new Vector2(0, 0.39f)), Is.EqualTo(new Point(0, 1)));

            Assert.That(_grid.PosToCell(new Vector2(0.3f, 0.3f)), Is.EqualTo(new Point(1, 1)));
	    }

	    [Test]
	    public void CellToPos()
	    {
            Assert.That(_grid.CellToPos(new Point(1, 0)), Is.EqualTo(new Vector2(0.3f, 0.1f)));
            Assert.That(_grid.CellToPos(new Point(0, 1)), Is.EqualTo(new Vector2(0.1f, 0.3f)));
            Assert.That(_grid.CellToPos(new Point(1, 1)), Is.EqualTo(new Vector2(0.3f, 0.3f)));
        }

	    [Test]
	    public void Covers()
	    {
	        Assert.That(_grid.Covers(new Vector2()));
            Assert.That(_grid.Covers(new Vector2() - new Vector2(float.Epsilon, float.Epsilon)), Is.False);

            Assert.That(_grid.Covers(new Vector2(0.6f, 0.4f) - new Vector2(1e-5f, 1e-5f)));
            Assert.That(_grid.Covers(new Vector2(0.6f, 0.4f)), Is.False);

            Assert.That(_grid.Covers(new Vector2(0.3f, 0.3f)));

            Assert.That(_grid.Covers(new Point()));
            Assert.That(_grid.Covers(new Point(-1, -1)), Is.False);

            Assert.That(_grid.Covers(new Point(_grid.SizeInCells.X - 1, _grid.SizeInCells.Y - 1)));
            Assert.That(_grid.Covers(_grid.SizeInCells), Is.False);

            Assert.That(_grid.Covers(new Point(1, 1)));
	    }
	}
}