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
		public void NoJumpPointsBecauseOfGridBoundaries()
		{
			var grid = new OccupancyGrid(new[,] { { false, false, false }, { false, false, false }, { false, false, false } }, 0.2f);

			var sp = Substitute.For<ISearchProblem<Point>>();
			sp.GoalState.Returns(new Point(3, 0));
			var jpg = new JumpPointGenerator(grid, sp);

			Assert.That(jpg.Generate(new Point(0, 0)), Is.Empty);
		}

		[Test]
		public void NoJumpPointsBecauseOfWalls()
		{
			var grid = new OccupancyGrid(new[,] { { false, true, false }, { false, true, false }, { false, true, false } }, 0.2f);

			var sp = Substitute.For<ISearchProblem<Point>>();
			sp.GoalState.Returns(new Point(2, 0));
			var jpg = new JumpPointGenerator(grid, sp);

			Assert.That(jpg.Generate(new Point(0, 0)), Is.Empty);
		}

		[Test]
		public void GoalJumpPoint()
		{
			var grid = new OccupancyGrid(new[,] { { false, false, false }, { false, false, false }, { false, false, false } }, 0.2f);

			var sp = Substitute.For<ISearchProblem<Point>>();
			sp.GoalState.Returns(new Point(2, 0));
			var jpg = new JumpPointGenerator(grid, sp);

			Assert.That(jpg.Generate(new Point(0, 0)), Is.EquivalentTo(new []{new Point(2, 0)}));
		}

		[Test]
		public void ForcedNeighbourJumpPoint()
		{
			var grid = new OccupancyGrid(new[,] { { false, false, false }, { false, true, false }, { false, false, false } }, 0.2f);
			//| | | |
			//| |O| |
			//|p|x| |

			var sp = Substitute.For<ISearchProblem<Point>>();
			sp.GoalState.Returns(new Point(3, 0));
			var jpg = new JumpPointGenerator(grid, sp);

			Assert.That(jpg.Generate(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 0) }));

			grid = new OccupancyGrid(new[,] { { false, false, false }, { false, false, false }, {false, false, false } }, 0.2f);
			//| | | |
			//| | |O|
			//|p| |x|

			jpg = new JumpPointGenerator(grid, sp);

			Assert.That(jpg.Generate(new Point(0, 0)), Is.Empty);

			grid = new OccupancyGrid(new[,] { { false, false, true }, { false, true, true }, { false, false, false } }, 0.2f);
			//| | | |
			//| |O|O|
			//|p|x|O|

			jpg = new JumpPointGenerator(grid, sp);

			Assert.That(jpg.Generate(new Point(0, 0)), Is.Empty);
		}

		[Test]
		[Ignore("Under construction...")]
		public void AllStraightDirectionsJumpPoints()
		{
			var grid = new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, true, false, false, false},
						{false, false, false, false, false},
						{false, false, false, true, false},
						{false, false, false, false, false}
					}, 0.2f);
			//| | | | | |
			//| | |x|O| |
			//| |x|p|x| |
			//| |O|x| | |
			//| | | | | |

			var sp = Substitute.For<ISearchProblem<Point>>();
			sp.GoalState.Returns(new Point(5, 0));
			var jpg = new JumpPointGenerator(grid, sp);

			Assert.That(jpg.Generate(new Point(2, 2)), Is.EquivalentTo(new[] { new Point(2, 1), new Point(1, 2), new Point(3, 2), new Point(2, 4) }));
		}

		[Test]
		[Ignore("To do")]
		public void QQ()
		{
			Assert.Fail("Contracts; Inflate map");
		}
	}
}