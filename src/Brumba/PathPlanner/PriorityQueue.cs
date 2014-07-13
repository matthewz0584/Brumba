using System;
using System.Collections.Generic;
using System.Linq;
using DC = System.Diagnostics.Contracts;

namespace Brumba.PathPlanner
{
	public class PriorityQueue<T> where T : IComparable<T>
	{
		private readonly List<T> _data = new List<T>();

        [DC.ContractInvariantMethod]
        void ObjectInvariant()
        {
            DC.Contract.Invariant(IsConsistent());
        }

		public void Enqueue(T item)
		{
            DC.Contract.Ensures(Count == DC.Contract.OldValue<int>(Count) + 1);

			_data.Add(item);
			int ci = _data.Count - 1; // child index; start at end
			while (ci > 0)
			{
				int pi = (ci - 1) / 2; // parent index
				if (_data[ci].CompareTo(_data[pi]) >= 0) break; // child item is larger than (or equal) parent so we're done
				Swap(ci, pi);
				ci = pi;
			}
		}

		public T Dequeue()
		{
            DC.Contract.Ensures(Count == DC.Contract.OldValue<int>(Count) - 1);

			// assumes pq is not empty; up to calling code
			int li = _data.Count - 1; // last index (before removal)
			T frontItem = _data[0];   // fetch the front
			_data[0] = _data[li];
			_data.RemoveAt(li);

			--li; // last index (after removal)
			int pi = 0; // parent index. start at front of pq
			while (true)
			{
				int ci = pi * 2 + 1; // left child index of parent
				if (ci > li) break;  // no children so done
				int rc = ci + 1;     // right child
				if (rc <= li && _data[rc].CompareTo(_data[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
					ci = rc;
				if (_data[pi].CompareTo(_data[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
				Swap(pi, ci);
				pi = ci;
			}
			return frontItem;
		}

		public T Peek()
		{
            DC.Contract.Ensures(Count == DC.Contract.OldValue<int>(Count));

			return _data[0];
		}

		public int Count
		{
		    get { return _data.Count; }
		}

		public override string ToString()
		{
			return _data.Aggregate("", (current, t) => current + t + " ") + "count = " + _data.Count;
		}

		public bool IsConsistent()
		{
			// is the heap property true for all data?
			if (_data.Count == 0) return true;
			int li = _data.Count - 1; // last index
			for (int pi = 0; pi < _data.Count; ++pi) // each parent index
			{
				int lci = 2 * pi + 1; // left child index
				int rci = 2 * pi + 2; // right child index

				if (lci <= li && _data[pi].CompareTo(_data[lci]) > 0) return false; // if lc exists and it's greater than parent then bad.
				if (rci <= li && _data[pi].CompareTo(_data[rci]) > 0) return false; // check the right child too.
			}
			return true; // passed all checks
		}

		void Swap(int ci, int pi)
		{
			T tmp = _data[ci];
			_data[ci] = _data[pi];
			_data[pi] = tmp;
		}
	}
}