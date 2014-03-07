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
	    public void PointIndexer()
	    {
            Assert.That(_grid[new Point(0, 0)], Is.False);
            Assert.That(_grid[new Point(1, 0)], Is.True);
            Assert.That(_grid[new Point(2, 0)], Is.False);

            Assert.That(_grid[new Point(0, 1)], Is.False);
            Assert.That(_grid[new Point(1, 1)], Is.False);
            Assert.That(_grid[new Point(2, 1)], Is.True);
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
	}
}