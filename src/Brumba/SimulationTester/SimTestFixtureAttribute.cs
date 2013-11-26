using System;

namespace Brumba.SimulationTester
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SimTestFixtureAttribute : Attribute
    {
        public string EnvironmentFile { get; set; }
        public bool Wip { get; set; }
        public bool Ignore { get; set; }

        public SimTestFixtureAttribute(string environmentFile, bool ignore = false, bool wip = false)
        {
            EnvironmentFile = environmentFile;
            Ignore = ignore;
            Wip = wip;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SimTestAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SimSetUpAttribute : Attribute
    {
    }
}