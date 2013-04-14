using System;

namespace Brumba.Simulation.SimulationTester
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SimulationTestFixtureAttribute : Attribute
    {
        public bool Wip { get; set; }
        public bool Ignore { get; set; }

        public SimulationTestFixtureAttribute(bool ignore = false, bool wip = false)
        {
            Ignore = ignore;
            Wip = wip;
        }
    }
}