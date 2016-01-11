using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
    [System.Diagnostics.Contracts.ContractClassAttribute(typeof(ICellExpanderContract))]
    public interface ICellExpander
    {
        IEnumerable<Point> Expand(Point from);
        Point Goal { get; set; }
    }

    [DC.ContractClassForAttribute(typeof(ICellExpander))]
    abstract class ICellExpanderContract : ICellExpander
    {
        public IEnumerable<Point> Expand(Point from)
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Point>>() != null);
            return default(IEnumerable<Point>);
        }

        public Point Goal { get; set; }
    }
}