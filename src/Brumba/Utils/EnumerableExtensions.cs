using System.Collections.Generic;
using System.Linq;
using DC = System.Diagnostics.Contracts;

namespace Brumba.Utils
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> AsCol<T>(this T me)
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<T>>() != null);

            return new[] {me};
        }

        public static IEnumerable<T> FilterSequencialDuplicates<T>(this IEnumerable<T> me)
        {
            DC.Contract.Requires(me != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<T>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<T>>().Count() <= me.Count());

            if (!me.Any())
                yield break;

            var prev = me.First();
            yield return prev;
            foreach (var cur in me.Skip(1))
                if (!prev.Equals(cur))
                {
                    yield return cur;
                    prev = cur;
                }
        }

        public static List<T> Addd<T>(this List<T> me, T toAdd)
        {
            DC.Contract.Requires(me != null);
            DC.Contract.Ensures(DC.Contract.Result<List<T>>() == me);

            me.Add(toAdd);
            return me;
        }
    }
}