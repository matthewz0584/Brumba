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
            var grid = new OccupancyGrid(new[,] { { false, false, false }, { false, false, false }, { false, false, false } }, 0.2f);

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(2, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(2, 0) }));
        }

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
        public void HorizontalForcedNeighbour()
        {
            var grid = new OccupancyGrid(new[,] { { false, false, true }, { false, false, false }, { false, true, false } }, 0.2f);
            //| |O| |
            //|p|x| |
            //| | |O|

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(3, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 1)), Is.EquivalentTo(new[] { new Point(1, 1) }));

            grid = new OccupancyGrid(new[,] { { false, true, false }, { false, false, false }, { false, false, false } }, 0.2f);
            //| | |O|
            //|p|x| |
            //| |O| |

            jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 1)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

        [Test]
        public void NotHorizontalForcedNeighbour()
        {
            var grid = new OccupancyGrid(new[,] { { false, false, false }, { false, false, false }, { false, true, true } }, 0.2f);
            //| |O|O|
            //|p|x| |
            //| | | |

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(3, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 1)), Is.Empty);

            grid = new OccupancyGrid(new[,] { { false, false, false }, { false, false, false }, { false, false, true } }, 0.2f);
            //| | |O|
            //| |p|x|
            //| | | |

            jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(1, 1)), Is.Empty);
        }

        [Test]
        public void VerticalForcedNeighbour()
        {
            var grid = new OccupancyGrid(new[,] { { false, false, false }, { true, false, false }, { false, false, false } }, 0.2f);
            //| |p| |
            //|O|x| |
            //| | | |

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(3, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(1, 2)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

		[Test]
		public void AllStraightDirections()
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

			Assert.That(jpg.Generate(new Point(2, 2)), Is.EquivalentTo(new[] { new Point(2, 1), new Point(1, 2), new Point(3, 2), new Point(2, 3) }));
		}

        [Test]
        public void DiagonalForcedNeighbour()
        {
            var grid = new OccupancyGrid(new[,] { { false, false, true }, { true, false, false }, { false, false, false } }, 0.2f);
            //| | | |
            //|O|x| |
            //|p| |O|

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(3, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));

            grid = new OccupancyGrid(new[,] { { false, true, false }, { false, false, false }, { true, false, false } }, 0.2f);
            //|O| | |
            //| |x| |
            //|p|O| |

            jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

        [Test]
        public void NotDiagonalForcedNeighbour()
        {
            var grid = new OccupancyGrid(new[,] { { false, false, false }, { true, false, false }, { true, false, false } }, 0.2f);
            //|O| | |
            //|O|x| |
            //|p| | |

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(3, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 0)), Is.Empty);

            grid = new OccupancyGrid(new[,] { { false, false, false }, { false, false, false }, { false, true, false } }, 0.2f);
            //| |O|x|
            //| |p| |
            //| | | |

            jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(1, 1)), Is.Empty);
        }

        [Test]
        public void AllDiagonalDirections()
        {
            var grid = new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false}
					}, 0.2f);
            //| | | | | |
            //| |x|O|x| |
            //| | |p| | |
            //| |x|O|x| |
            //| | | | | |

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(5, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(2, 2)), Is.EquivalentTo(new[] { new Point(1, 1), new Point(3, 1), new Point(3, 3), new Point(1, 3) }));
        }

        [Test]
        public void DistantDiagonalAndStraightDirectionsForcedNeighbours()
        {
            var grid = new OccupancyGrid(new[,]
					{
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false},
						{false, true, true, false, false},
						{false, false, false, false, false}
					}, 0.2f);
            //| | | | | |
            //|x|O| | | |
            //| | |x| | |
            //| | |O| | |
            //|p| |x| | |

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(5, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(2, 0), new Point(2, 2), new Point(0, 3) }));
        }

        [Test]
        public void DiagonalRecursive()
        {
            var grid = new OccupancyGrid(new[,]
					{
						{false, false, false, false},
						{false, false, false, true},
						{true, false, false, false},
						{false, false, false, false}
					}, 0.2f);
            //| | | | |
            //|O| | | |
            //| |x| | |
            //|p| | | |

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(5, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));

            grid = new OccupancyGrid(new[,]
					{
						{false, false, false, false},
						{false, false, false, true},
						{true, false, false, false},
						{false, false, false, false}
					}, 0.2f);
            //| | | | |
            //| | | | |
            //| |x| | |
            //|p| |O| |

            jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(0, 0)), Is.EquivalentTo(new[] { new Point(1, 1) }));
        }

        [Test]
        public void All()
        {
            var grid = new OccupancyGrid(new[,]
					{
						{false, true, false, true, false},
						{false, true, false, false, false},
						{false, false, false, false, false},
						{false, false, true, false, false},
						{false, false, false, false, false}
					}, 0.2f);
            //| | | | | |
            //| | |O| | |
            //| |x| |x| |
            //| |O|p|x| |
            //| |O| |O| |

            var sp = Substitute.For<ISearchProblem<Point>>();
            sp.GoalState.Returns(new Point(5, 0));
            var jpg = new JumpPointGenerator(grid, sp);

            Assert.That(jpg.Generate(new Point(2, 1)), Is.EquivalentTo(new[] { new Point(3, 1), new Point(3, 2), new Point(1, 2) }));
        }

		[Test]
		[Ignore("To do")]
		public void QQ()
		{
			Assert.Fail("Contracts; Inflate map");
		}
	}
}