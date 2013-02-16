using System;

namespace Brumba.Simulation.SimulationTester
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SimulationTestFixtureAttribute : Attribute
    {
        public bool Ignore { get; private set; }

        public SimulationTestFixtureAttribute(bool ignore = false)
        {
            Ignore = ignore;
        }
    }
}