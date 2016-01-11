using System;
using System.Collections.Generic;

namespace Brumba.SimulationTestRunner
{
    class ServiceUriComparer : IEqualityComparer<Uri>
    {
        public bool Equals(Uri x, Uri y)
        {
            return x.Segments[1].Trim('/') == y.Segments[1].Trim('/');
        }

        public int GetHashCode(Uri obj)
        {
            return obj.Segments[1].Trim('/').GetHashCode();
        }
    }
}