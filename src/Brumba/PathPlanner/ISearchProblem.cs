using System;
using System.Collections.Generic;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
    [DC.ContractClassAttribute(typeof(ISearchProblemContract<>))]
    public interface ISearchProblem<T>
    {
        T InitialState { get; set; }
        T GoalState { get; set; }
        IEnumerable<Tuple<T, int>> Expand(T state);
        int GetHeuristic(T state);
    }

    [DC.ContractClassForAttribute(typeof(ISearchProblem<>))]
    abstract class ISearchProblemContract<T> : ISearchProblem<T>
    {
        public T InitialState { get; set; }
        public T GoalState { get; set; }

        public IEnumerable<Tuple<T, int>> Expand(T state)
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Tuple<T, int>>>() != null);
            return default(IEnumerable<Tuple<T, int>>);
        }

        public int GetHeuristic(T state)
        {
            DC.Contract.Ensures(DC.Contract.Result<int>() >= 0);
            return default(int);
        }
    }
}