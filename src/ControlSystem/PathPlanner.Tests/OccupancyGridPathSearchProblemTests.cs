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

            var gpsp = new OccupancyGridPathSearchProblem(map: map, cellExpander: new JumpPointGenerator(map))
            {
                InitialState = new Point(0, 0),
                GoalState = new Point(3, 0)
            };

            Assert.That(gpsp.InitialState, Is.EqualTo(new Point(0, 0)));
            Assert.That(gpsp.GoalState, Is.EqualTo(new Point(3, 0)));
            var successors = gpsp.Expand(new Point(0, 0));
            Assert.That(successors, Is.EquivalentTo(new[] { Tuple.Create(new Point(1, 1), Math.Sqrt(1 + 1)), Tuple.Create(new Point(3, 0), 3.0) }));
            Assert.That(gpsp.GetHeuristic(new Point(1, 1)), Is.EqualTo(Math.Sqrt(2 * 2 + 1 * 1)));
        }

        [Test]
        public void GoalState()
        {
            var expander = Substitute.For<ICellExpander>();
            var gpsp = new OccupancyGridPathSearchProblem(new OccupancyGrid(), expander);
            
            gpsp.GoalState = new Point(1, 2);

            Assert.That(expander.Goal, Is.EqualTo(new Point(1, 2)));
        }

        [Test]
        public void Expand()
        {
            var expander = Substitute.For<ICellExpander>();
            var gpsp = new OccupancyGridPathSearchProblem(new OccupancyGrid(), expander);

            expander.Expand(new Point(2, 1)).Returns(new [] { new Point(3, 5), new Point(10, 9) });
            Assert.That(gpsp.Expand(new Point(2, 1)), Is.EquivalentTo(new[] { Tuple.Create(new Point(3, 5), Math.Sqrt(1 * 1 + 4 * 4)), Tuple.Create(new Point(10, 9), Math.Sqrt(8 * 8 + 8 * 8)) }));

            expander.Expand(new Point(2, 3)).Returns(new Point[0]);
            Assert.That(gpsp.Expand(new Point(2, 3)), Is.Empty);
        }

        [Test]
        public void GetHeuristic()
        {
            var gpsp = new OccupancyGridPathSearchProblem(new OccupancyGrid(), Substitute.For<ICellExpander>())
            {
                InitialState = new Point(),
                GoalState = new Point(5, 4)
            };

            Assert.That(gpsp.GetHeuristic(new Point(1, 2)), Is.EqualTo(Math.Sqrt(4 * 4 + 2 * 2)));
            Assert.That(gpsp.GetHeuristic(new Point(5, 4)), Is.EqualTo(0));
        }
    }
}