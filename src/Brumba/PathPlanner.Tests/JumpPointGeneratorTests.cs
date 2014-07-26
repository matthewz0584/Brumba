using Brumba.MapProvider;
using Microsoft.Xna.Framework;
using NSubstitute;
using NUnit.Framework;

namespace Brumba.PathPlanner.Tests
{
	[TestFixture]
	public class JumpPointGeneratorTests
	{
	    [Test]
        public void Goal()
	    {
	        var jpg = new JumpPointGenerator(new OccupancyGrid(
	            new[,] {{false, false, false}, {false, false, false}, {false, false, false}}, 0.2f))
	            { Goal = new Point(2, 0) };

            Assert.That(jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(2, 0) }));
        }

		[Test]
		public void NoJumpPointsBecauseOfGridBoundaries()
		{
		    var jpg = new JumpPointGenerator(new OccupancyGrid(
		        new[,] {{false, false, false}, {false, false, false}, {false, false, false}}, 0.2f))
                { Goal = new Point(3, 0) };

			Assert.That(jpg.Expand(new Point(0, 0)), Is.Empty);
		}

		[Test]
		public void NoJumpPointsBecauseOfWalls()
		{
		    var jpg = new JumpPointGenerator(new OccupancyGrid(
		        new[,] {{false, true, false}, {false, true, false}, {false, true, false}}, 0.2f))
                {Goal = new Point(2, 0)};

			Assert.That(jpg.Expand(new Point(0, 0)), Is.Empty);
		}

        [Test]
        public void HorizontalForcedNeighbour()
        {
            //| |O| |
            //|p|x| |
            //| | |O|
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
            {
                { false, false, true },
                { false, false, false },
                { false, true, false }
            }, 0.2f)) { Goal = new Point(3, 0) };

            Assert.That(jpg.Expand(new Point(0, 1)), Is.EquivalentTo(new[] { new Point(1, 1) }));

            //| | |O|
            //|p|x| |
            //| |O| |
            jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
            {
                { false, true, false },
                { false, false, false },
                { false, false, false }
            }, 0.2f)) { Goal = new Point(3, 0) };

            Assert.That(jpg.Expand(new Point(0, 1)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

        [Test]
        public void NotHorizontalForcedNeighbour()
        {
            //| |O|O|
            //|p|x| |
            //| | | |
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { false, false, false },
                { false, true, true }
            }, 0.2f)) { Goal = new Point(3, 0) };

            Assert.That(jpg.Expand(new Point(0, 1)), Is.Empty);

            //| | |O|
            //| |p|x|
            //| | | |
            jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { false, false, false },
                { false, false, true }
            }, 0.2f)) { Goal = new Point(3, 0) };

            Assert.That(jpg.Expand(new Point(1, 1)), Is.Empty);
        }

        [Test]
        public void VerticalForcedNeighbour()
        {
            //| |p| |
            //|O|x| |
            //| | | |
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { true, false, false },
                { false, false, false }
            }, 0.2f)) { Goal = new Point(3, 0) };

            Assert.That(jpg.Expand(new Point(1, 2)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

		[Test]
		public void AllStraightDirections()
		{
			//| | | | | |
			//| | |x|O| |
			//| |x|p|x| |
			//| |O|x| | |
			//| | | | | |
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, true, false, false, false},
						{false, false, false, false, false},
						{false, false, false, true, false},
						{false, false, false, false, false}
					}, 0.2f)) { Goal = new Point(5, 0) };

			Assert.That(jpg.Expand(new Point(2, 2)), Is.EquivalentTo(new[] { new Point(2, 1), new Point(1, 2), new Point(3, 2), new Point(2, 3) }));
		}

        [Test]
        public void DiagonalForcedNeighbour()
        {
            //| | | |
            //|O|x| |
            //|p| |O|
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
            {
                { false, false, true },
                { true, false, false },
                { false, false, false }
            }, 0.2f)) { Goal = new Point(3, 0) };

            Assert.That(jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));

            //|O| | |
            //| |x| |
            //|p|O| |
            jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
            {
                { false, true, false },
                { false, false, false },
                { true, false, false }
            }, 0.2f)) { Goal = new Point(3, 0) };;

            Assert.That(jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

        [Test]
        public void NotDiagonalForcedNeighbour()
        {
            //|O| | |
            //|O|x| |
            //|p| | |
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { true, false, false },
                { true, false, false }
            }, 0.2f)) { Goal = new Point(3, 0) };;

            Assert.That(jpg.Expand(new Point(0, 0)), Is.Empty);

            //| |O|x|
            //| |p| |
            //| | | |
            jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
            {
                { false, false, false },
                { false, false, false },
                { false, true, false }
            }, 0.2f)) { Goal = new Point(3, 0) };;

            Assert.That(jpg.Expand(new Point(1, 1)), Is.Empty);
        }

        [Test]
        public void AllDiagonalDirections()
        {
            //| | | | | |
            //| |x|O|x| |
            //| | |p| | |
            //| |x|O|x| |
            //| | | | | |
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false}
					}, 0.2f)) { Goal = new Point(5, 0) };;

            Assert.That(jpg.Expand(new Point(2, 2)), Is.EquivalentTo(new[] { new Point(1, 1), new Point(3, 1), new Point(3, 3), new Point(1, 3) }));
        }

        [Test]
        public void DistantDiagonalAndStraightDirectionsForcedNeighbours()
        {
            //| | | | | |
            //|x|O| | | |
            //| | |x| | |
            //| | |O| | |
            //|p| |x| | |
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false},
						{false, true, true, false, false},
						{false, false, false, false, false}
					}, 0.2f)) { Goal = new Point(5, 0) };;

            Assert.That(jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(2, 0), new Point(2, 2), new Point(0, 3) }));
        }

        [Test]
        public void DiagonalRecursive()
        {
            //| | | | |
            //|O| | | |
            //| |x| | |
            //|p| | | |
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
					{
						{false, false, false, false},
						{false, false, false, true},
						{true, false, false, false},
						{false, false, false, false}
					}, 0.2f)) { Goal = new Point(5, 0) };;

            Assert.That(jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));

            //| | | | |
            //| | | | |
            //| |x| | |
            //|p| |O| |
            jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
					{
						{false, false, false, false},
						{false, false, false, true},
						{true, false, false, false},
						{false, false, false, false}
					}, 0.2f)) { Goal = new Point(5, 0) };;

            Assert.That(jpg.Expand(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

        [Test]
        public void All()
        {
            //| | | | | |
            //| | |O| | |
            //| |x| |x| |
            //| |O|p|x| |
            //| |O| |O| |
            var jpg = new JumpPointGenerator(new OccupancyGrid(new[,]
					{
						{false, true, false, true, false},
						{false, true, false, false, false},
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false}
					}, 0.2f)) { Goal = new Point(5, 0) }; ;

            Assert.That(jpg.Expand(new Point(2, 1)), Is.EquivalentTo(new[] { new Point(3, 1), new Point(3, 2), new Point(1, 2) }));
        }
	}
}