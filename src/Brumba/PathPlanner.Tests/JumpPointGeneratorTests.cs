using Brumba.MapProvider;
using Microsoft.Xna.Framework;
using NSubstitute;
using NUnit.Framework;

namespace Brumba.PathPlanner.Tests
{
	[TestFixture]
	public class JumpPointGeneratorTests
	{
	    private IOccupancyGridPathSearchProblem _sp;
	    private JumpPointGenerator _jpg;

	    [SetUp]
	    public void SetUp()
	    {
	        _sp = Substitute.For<IOccupancyGridPathSearchProblem>();
	        _jpg = new JumpPointGenerator(_sp);
	    }

	    [Test]
        public void Goal()
        {
	        _sp.GoalState.Returns(new Point(2, 0));
	        _sp.Map.Returns(new OccupancyGrid(
	            new[,] {{false, false, false}, {false, false, false}, {false, false, false}}, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(2, 0) }));
        }

		[Test]
		public void NoJumpPointsBecauseOfGridBoundaries()
		{
		    _sp.GoalState.Returns(new Point(3, 0));
		    _sp.Map.Returns(new OccupancyGrid(
                new[,] {{false, false, false}, {false, false, false}, {false, false, false}}, 0.2f));

			Assert.That(_jpg.Expand(new Point(0, 0)), Is.Empty);
		}

		[Test]
		public void NoJumpPointsBecauseOfWalls()
		{
		    _sp.GoalState.Returns(new Point(2, 0));
            _sp.Map.Returns(new OccupancyGrid(
                new[,] { { false, true, false }, { false, true, false }, { false, true, false } }, 0.2f));

			Assert.That(_jpg.Expand(new Point(0, 0)), Is.Empty);
		}

        [Test]
        public void HorizontalForcedNeighbour()
        {
            //| |O| |
            //|p|x| |
            //| | |O|
            _sp.GoalState.Returns(new Point(3, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
            {
                { false, false, true },
                { false, false, false },
                { false, true, false }
            }, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 1)), Is.EquivalentTo(new[] { new Point(1, 1) }));

            //| | |O|
            //|p|x| |
            //| |O| |
            _sp.Map.Returns(new OccupancyGrid(new[,]
            {
                { false, true, false },
                { false, false, false },
                { false, false, false }
            }, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 1)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

        [Test]
        public void NotHorizontalForcedNeighbour()
        {
            //| |O|O|
            //|p|x| |
            //| | | |
            _sp.GoalState.Returns(new Point(3, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { false, false, false },
                { false, true, true }
            }, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 1)), Is.Empty);

            //| | |O|
            //| |p|x|
            //| | | |
            _sp.Map.Returns(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { false, false, false },
                { false, false, true }
            }, 0.2f));

            Assert.That(_jpg.Expand(new Point(1, 1)), Is.Empty);
        }

        [Test]
        public void VerticalForcedNeighbour()
        {
            //| |p| |
            //|O|x| |
            //| | | |
            _sp.GoalState.Returns(new Point(3, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { true, false, false },
                { false, false, false }
            }, 0.2f));

            Assert.That(_jpg.Expand(new Point(1, 2)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

		[Test]
		public void AllStraightDirections()
		{
			//| | | | | |
			//| | |x|O| |
			//| |x|p|x| |
			//| |O|x| | |
			//| | | | | |
			_sp.GoalState.Returns(new Point(5, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, true, false, false, false},
						{false, false, false, false, false},
						{false, false, false, true, false},
						{false, false, false, false, false}
					}, 0.2f));

			Assert.That(_jpg.Expand(new Point(2, 2)), Is.EquivalentTo(new[] { new Point(2, 1), new Point(1, 2), new Point(3, 2), new Point(2, 3) }));
		}

        [Test]
        public void DiagonalForcedNeighbour()
        {
            //| | | |
            //|O|x| |
            //|p| |O|
            _sp.GoalState.Returns(new Point(3, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
            {
                { false, false, true },
                { true, false, false },
                { false, false, false }
            }, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));

            //|O| | |
            //| |x| |
            //|p|O| |
            _sp.Map.Returns(new OccupancyGrid(new[,]
            {
                { false, true, false },
                { false, false, false },
                { true, false, false }
            }, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

        [Test]
        public void NotDiagonalForcedNeighbour()
        {
            //|O| | |
            //|O|x| |
            //|p| | |
            _sp.GoalState.Returns(new Point(3, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { true, false, false },
                { true, false, false }
            }, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 0)), Is.Empty);

            //| |O|x|
            //| |p| |
            //| | | |
            _sp.Map.Returns(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { false, false, false },
                { false, true, false }
            }, 0.2f));

            Assert.That(_jpg.Expand(new Point(1, 1)), Is.Empty);
        }

        [Test]
        public void AllDiagonalDirections()
        {
            //| | | | | |
            //| |x|O|x| |
            //| | |p| | |
            //| |x|O|x| |
            //| | | | | |
            _sp.GoalState.Returns(new Point(5, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false}
					}, 0.2f));

            Assert.That(_jpg.Expand(new Point(2, 2)), Is.EquivalentTo(new[] { new Point(1, 1), new Point(3, 1), new Point(3, 3), new Point(1, 3) }));
        }

        [Test]
        public void DistantDiagonalAndStraightDirectionsForcedNeighbours()
        {
            //| | | | | |
            //|x|O| | | |
            //| | |x| | |
            //| | |O| | |
            //|p| |x| | |
            _sp.GoalState.Returns(new Point(5, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false},
						{false, true, true, false, false},
						{false, false, false, false, false}
					}, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(2, 0), new Point(2, 2), new Point(0, 3) }));
        }

        [Test]
        public void DiagonalRecursive()
        {
            //| | | | |
            //|O| | | |
            //| |x| | |
            //|p| | | |
            _sp.GoalState.Returns(new Point(5, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
					{
						{false, false, false, false},
						{false, false, false, true},
						{true, false, false, false},
						{false, false, false, false}
					}, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));

            //| | | | |
            //| | | | |
            //| |x| | |
            //|p| |O| |
            _sp.Map.Returns(new OccupancyGrid(new[,]
					{
						{false, false, false, false},
						{false, false, false, true},
						{true, false, false, false},
						{false, false, false, false}
					}, 0.2f));

            Assert.That(_jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

        [Test]
        public void All()
        {
            //| | | | | |
            //| | |O| | |
            //| |x| |x| |
            //| |O|p|x| |
            //| |O| |O| |
            _sp.GoalState.Returns(new Point(5, 0));
            _sp.Map.Returns(new OccupancyGrid(new[,]
					{
						{false, true, false, true, false},
						{false, true, false, false, false},
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false}
					}, 0.2f));

            Assert.That(_jpg.Expand(new Point(2, 1)), Is.EquivalentTo(new[] { new Point(3, 1), new Point(3, 2), new Point(1, 2) }));
        }

		[Test]
		[Ignore("To do")]
		public void QQ()
		{
			Assert.Fail("Contracts; Inflate map");
		}
	}
}