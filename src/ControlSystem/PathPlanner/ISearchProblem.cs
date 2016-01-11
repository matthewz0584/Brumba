using System;
using System.Collections.Generic;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
    [DC.ContractClassAttribute(typeof(ISearchProblemContract<>))]
    public interface ISearchProblem<TState>
    {
        TState InitialState { get; set; }
        TState GoalState { get; set; }
        IEnumerable<Tuple<TState, double>> Expand(TState state);
        double GetHeuristic(TState state);
    }

    [DC.ContractClassForAttribute(typeof(ISearchProblem<>))]
    abstract class ISearchProblemContract<TState> : ISearchProblem<TState>
    {
        public TState InitialState { get; set; }
        public TState GoalState { get; set; }

        public IEnumerable<Tuple<TState, double>> Expand(TState state)
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Tuple<TState, double>>>() != null);
            return default(IEnumerable<Tuple<TState, double>>);
        }

        public double GetHeuristic(TState state)
        {
            DC.Contract.Ensures(DC.Contract.Result<double>() >= 0);
            return default(double);
        }
    }
}