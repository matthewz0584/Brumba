using System;
using System.Collections.Generic;

namespace Brumba.PathPlanner.Tests
{
	public class TouringRomaniaProblem : ISearchProblem<string>
	{
        readonly Dictionary<string, double> _straightLineDistanceHeurstic = new Dictionary<string, double>
		{
			{"Arad", 366}, {"Bucharest", 0}, {"Craiova", 160}, {"Dobreta", 242}, {"Eforie", 161}, {"Fagaras", 176},
			{"Giurgiu", 77}, {"Hirsova", 151}, {"Iasi", 226}, {"Lugoj", 244}, {"Mehadia", 241}, {"Neamt", 234},
			{"Oradea", 380}, {"Pitesti", 100}, {"Rimnicu Vilcea", 193}, {"Sibiu", 253}, {"Timisoara", 329},
			{"Urziceni", 80}, {"Vaslui", 199}, {"Zerind", 374}
		};

        readonly Dictionary<string, List<Tuple<string, double>>> _connectivity = new Dictionary<string, List<Tuple<string, double>>>
		{
			{"Arad", new List<Tuple<string, double>> { Tuple.Create("Zerind", 75.0), Tuple.Create("Timisoara", 118.0), Tuple.Create("Sibiu", 140.0)}},
			{"Sibiu", new List<Tuple<string, double>> { Tuple.Create("Arad", 140.0), Tuple.Create("Oradea", 151.0), Tuple.Create("Fagaras", 99.0), Tuple.Create("Rimnicu Vilcea", 80.0)}},
			{"Fagaras", new List<Tuple<string, double>> { Tuple.Create("Sibiu", 99.0), Tuple.Create("Bucharest", 211.0)}},
			{"Rimnicu Vilcea", new List<Tuple<string, double>> { Tuple.Create("Sibiu", 80.0), Tuple.Create("Pitesti", 97.0), Tuple.Create("Craiova", 146.0)}},
			{"Pitesti", new List<Tuple<string, double>> { Tuple.Create("Rimnicu Vilcea", 97.0), Tuple.Create("Bucharest", 101.0), Tuple.Create("Craiova", 138.0)}},
			{"Timisoara", new List<Tuple<string, double>> { Tuple.Create("Arad", 118.0), Tuple.Create("Lugoj", 111.0)}},
			{"Lugoj", new List<Tuple<string, double>> { Tuple.Create("Timisoara", 111.0), Tuple.Create("Mehadia", 70.0)}},
			{"Mehadia", new List<Tuple<string, double>> { Tuple.Create("Lugoj", 70.0), Tuple.Create("Dobreta", 75.0)}},
			{"Dobreta", new List<Tuple<string, double>> { Tuple.Create("Mehadia", 75.0), Tuple.Create("Craiova", 120.0)}},
			{"Craiova", new List<Tuple<string, double>> { Tuple.Create("Dobreta", 120.0), Tuple.Create("Rimnicu Vilcea", 146.0), Tuple.Create("Pitesti", 138.0)}}
		};

		public string InitialState { get; set; }
		public string GoalState { get; set; }

        public IEnumerable<Tuple<string, double>> Expand(string state)
		{
			return _connectivity[state];
		}

        public double GetHeuristic(string state)
		{
			return _straightLineDistanceHeurstic[state];
		}
	}
}