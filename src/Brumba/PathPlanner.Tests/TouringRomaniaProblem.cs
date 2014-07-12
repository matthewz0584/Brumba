using System;
using System.Collections.Generic;

namespace Brumba.PathPlanner.Tests
{
	public class TouringRomaniaProblem : ISearchProblem<string>
	{
		readonly Dictionary<string, int> _straightLineDistanceHeurstic = new Dictionary<string, int>
		{
			{"Arad", 366}, {"Bucharest", 0}, {"Craiova", 160}, {"Dobreta", 242}, {"Eforie", 161}, {"Fagaras", 176},
			{"Giurgiu", 77}, {"Hirsova", 151}, {"Iasi", 226}, {"Lugoj", 244}, {"Mehadia", 241}, {"Neamt", 234},
			{"Oradea", 380}, {"Pitesti", 100}, {"Rimnicu Vilcea", 193}, {"Sibiu", 253}, {"Timisoara", 329},
			{"Urziceni", 80}, {"Vaslui", 199}, {"Zerind", 374}
		};

		readonly Dictionary<string, List<Tuple<string, int>>> _connectivity = new Dictionary<string, List<Tuple<string, int>>>
		{
			{"Arad", new List<Tuple<string, int>> { Tuple.Create("Zerind", 75), Tuple.Create("Timisoara", 118), Tuple.Create("Sibiu", 140)}},
			{"Sibiu", new List<Tuple<string, int>> { Tuple.Create("Arad", 140), Tuple.Create("Oradea", 151), Tuple.Create("Fagaras", 99), Tuple.Create("Rimnicu Vilcea", 80)}},
			{"Fagaras", new List<Tuple<string, int>> { Tuple.Create("Sibiu", 99), Tuple.Create("Bucharest", 211)}},
			{"Rimnicu Vilcea", new List<Tuple<string, int>> { Tuple.Create("Sibiu", 80), Tuple.Create("Pitesti", 97), Tuple.Create("Craiova", 146)}},
			{"Pitesti", new List<Tuple<string, int>> { Tuple.Create("Rimnicu Vilcea", 97), Tuple.Create("Bucharest", 101), Tuple.Create("Craiova", 138)}},
			{"Timisoara", new List<Tuple<string, int>> { Tuple.Create("Arad", 118), Tuple.Create("Lugoj", 111)}},
			{"Lugoj", new List<Tuple<string, int>> { Tuple.Create("Timisoara", 111), Tuple.Create("Mehadia", 70)}},
			{"Mehadia", new List<Tuple<string, int>> { Tuple.Create("Lugoj", 70), Tuple.Create("Dobreta", 75)}},
			{"Dobreta", new List<Tuple<string, int>> { Tuple.Create("Mehadia", 75), Tuple.Create("Craiova", 120)}},
			{"Craiova", new List<Tuple<string, int>> { Tuple.Create("Dobreta", 120), Tuple.Create("Rimnicu Vilcea", 146), Tuple.Create("Pitesti", 138)}}
		};

		public string InitialState { get; set; }
		public string GoalState { get; set; }

		public IEnumerable<Tuple<string, int>> Expand(string state)
		{
			return _connectivity[state];
		}

		public int GetHeuristic(string state)
		{
			return _straightLineDistanceHeurstic[state];
		}
	}
}