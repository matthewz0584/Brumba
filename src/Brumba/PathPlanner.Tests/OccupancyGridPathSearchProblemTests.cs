using System;
using Brumba.MapProvider;
using Microsoft.Xna.Framework;
using NSubstitute;
using NUnit.Framework;

namespace Brumba.PathPlanner.Tests
{
    [TestFixture]
    public class OccupancyGridPathSearchProblemTests
    {
        [Test]
        public void Acceptance()
        {
            var map = new OccupancyGrid(new[,]
            {
                {false, false, false, false},
                {false, false, false, false},
                {true, false, false, false},
                {false, false, false, false}
            }, 0.2f);
            //| | | | |
            //|O| | | |
            //| |x| | |
            //|p| | |G|

            var gpsp = new OccupancyGridPathSearchProblem(map: map, start: new Point(0, 0), goal: new Point(3, 0));
            gpsp.CellExpander = new JumpPointGenerator(gpsp);

            Assert.That(gpsp.InitialState, Is.EqualTo(new Point(0, 0)));
            Assert.That(gpsp.GoalState, Is.EqualTo(new Point(3, 0)));
            var successors = gpsp.Expand(new Point(0, 0));
            Assert.That(successors, Is.EquivalentTo(new []{ Tuple.Create(new Point(1, 1), 1 + 1), Tuple.Create(new Point(3, 0), 3 * 3) }));
            Assert.That(gpsp.GetHeuristic(new Point(1, 1)), Is.EqualTo(2 * 2 + 1 * 1));
        }

        [Test]
        public void Expand()
        {
            var expander = Substitute.For<IStateExpander>();
            var gpsp = new OccupancyGridPathSearchProblem(new OccupancyGrid(), new Point(), new Point())
            {CellExpander = expander};

            expander.Expand(new Point(2, 1)).Returns(new [] { new Point(3, 5), new Point(10, 9) });
            Assert.That(gpsp.Expand(new Point(2, 1)), Is.EquivalentTo(new[] { Tuple.Create(new Point(3, 5), 1 * 1 + 4 * 4), Tuple.Create(new Point(10, 9), 8 * 8 + 8 * 8) }));

            expander.Expand(new Point(2, 3)).Returns(new Point[0]);
            Assert.That(gpsp.Expand(new Point(2, 3)), Is.Empty);
        }

        [Test]
        public void GetHeuristic()
        {
            var gpsp = new OccupancyGridPathSearchProblem(new OccupancyGrid(), new Point(), new Point(5, 4));

            Assert.That(gpsp.GetHeuristic(new Point(1, 2)), Is.EqualTo(4 * 4 + 2 * 2));
            Assert.That(gpsp.GetHeuristic(new Point(5, 4)), Is.EqualTo(0));
        }
    }
}