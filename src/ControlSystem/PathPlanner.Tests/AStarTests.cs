using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Brumba.PathPlanner.Tests
{
	[TestFixture]
	public class AStarTests
	{
		[Test]
		public void InitialStateIsGoal()
		{
			var astar = new AStar<int>(new IntSearchProblem { InitialState = 1, GoalState = 1, ExpandF = i => new List<int>(), HeuristicF = i => 0 });
			var path = astar.GraphSearch();
			Assert.That(path.Count(), Is.EqualTo(0));
		}

		[Test]
		public void SequenceSearch()
		{
			var astar = new AStar<int>(new IntSearchProblem { InitialState = 1, GoalState = 3, ExpandF = i => new List<int> { i + 1 }, HeuristicF = i => 0 });
			var path = astar.GraphSearch();
			Assert.That(path, Is.EquivalentTo(new[] { 2, 3 }));
		}

		[Test]
		public void TreeSearch()
		{
			var astar = new AStar<int>(new IntSearchProblem { InitialState = 1, GoalState = 3, ExpandF = i => Enumerable.Range(1, i + 1).ToList(), HeuristicF = i => 0 });
			var path = astar.GraphSearch();
			Assert.That(path, Is.EquivalentTo(new[] { 2, 3 }));
		}

		[Test]
		public void TreeSearchWithHeuristic()
		{
			var astar = new AStar<int>(new IntSearchProblem { InitialState = 1, GoalState = 3, ExpandF = i => Enumerable.Range(1, i + 1).ToList(), HeuristicF = i => 100 / i });
			var path = astar.GraphSearch();
			Assert.That(path, Is.EquivalentTo(new[] { 2, 3 }));
		}

		[Test]
		public void GraphSearch()
		{
			var astar = new AStar<int>(new IntSearchProblem { InitialState = 1, GoalState = 3, ExpandF = i => new List<int> { -i }, HeuristicF = i => 0 });
			var path = astar.GraphSearch();
			Assert.That(path, Is.Null);
		}

		[Test]
		public void Romania()
		{
			var astar = new AStar<string>(new TouringRomaniaProblem
			{
				InitialState = "Timisoara",
				GoalState = "Bucharest"
			});

			Assert.That(astar.GraphSearch(), Is.EquivalentTo(new[] { "Pitesti", "Rimnicu Vilcea", "Sibiu", "Arad", "Bucharest" }));
		}
	}

	class IntSearchProblem : ISearchProblem<int>
	{
		public int InitialState { get; set; }
		public int GoalState { get; set; }
		public Func<int, List<int>> ExpandF { get; set; }
		public Func<int, int> HeuristicF { get; set; }

		public IEnumerable<Tuple<int, double>> Expand(int state) { return ExpandF(state).Select(s => Tuple.Create(s, 1.0)); }

		public double GetHeuristic(int state) { return HeuristicF(state); }
	}
}