using System;
using System.Collections.Generic;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
	public class AStar<StateT>
	{
		private readonly ISearchProblem<StateT> _problem;

		public AStar(ISearchProblem<StateT> problem)
		{
            DC.Contract.Requires(problem != null);
			_problem = problem;
		}

		public List<StateT> GraphSearch()
		{
			var fringe = new PriorityQueue<SearchNode>();
			fringe.Enqueue(new SearchNode { State = _problem.InitialState, Value = _problem.GetHeuristic(_problem.InitialState) });
			var visited = new HashSet<StateT> {fringe.Peek().State};
			while (fringe.Count != 0)
			{
				var current = fringe.Dequeue();
				if (current.State.Equals(_problem.GoalState))
					return PathTo(current);
				visited.Add(current.State);
				foreach (var childStateAndCost in _problem.Expand(current.State))
				{
					if (visited.Contains(childStateAndCost.Item1))//ought to add check for presence in fringe also
						continue;
					var cost = current.Cost + childStateAndCost.Item2;
					fringe.Enqueue(new SearchNode
					{
						State = childStateAndCost.Item1,
						Parent = current,
						Cost = cost,
						Value = cost + _problem.GetHeuristic(childStateAndCost.Item1)
					});					
				}
			}
			return null;
		}

		static List<StateT> PathTo(SearchNode node)
		{
			var path = new List<StateT>();
			while (node.Parent != null)
			{
				path.Add(node.State);
				node = node.Parent;
			}
			path.Reverse();
			return path;
		}

		class SearchNode : IComparable<SearchNode>
		{
			public SearchNode Parent { get; set; }
			public double Value { get; set; }
			public StateT State { get; set; }
            public double Cost { get; set; }

			public int CompareTo(SearchNode other)
			{
				return Value < other.Value ? -1 : Value > other.Value ? 1 : 0;
			}
		}
	}
}