using System;
using System.Collections.Generic;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
    [DC.ContractClassAttribute(typeof(ISearchProblemContract<>))]
    public interface ISearchProblem<StateT>
    {
        StateT InitialState { get; set; }
        StateT GoalState { get; set; }
        IEnumerable<Tuple<StateT, double>> Expand(StateT state);
        double GetHeuristic(StateT state);
    }

    [DC.ContractClassForAttribute(typeof(ISearchProblem<>))]
    abstract class ISearchProblemContract<StateT> : ISearchProblem<StateT>
    {
        public StateT InitialState { get; set; }
        public StateT GoalState { get; set; }

        public IEnumerable<Tuple<StateT, double>> Expand(StateT state)
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Tuple<StateT, double>>>() != null);
            return default(IEnumerable<Tuple<StateT, double>>);
        }

        public double GetHeuristic(StateT state)
        {
            DC.Contract.Ensures(DC.Contract.Result<double>() >= 0);
            return default(double);
        }
    }
}