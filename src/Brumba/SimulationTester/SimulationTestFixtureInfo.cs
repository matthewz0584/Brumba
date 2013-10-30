using System;
using System.Collections.Generic;

namespace Brumba.Simulation.SimulationTester
{
    public class SimulationTestFixtureInfo
    {
        public SimulationTestFixtureInfo()
        {
            Tests = new List<ISimulationTest>();
            EnvironmentXmlFile = "";
            SetUp = _ => { };
        }

        public string EnvironmentXmlFile { get; set; }
        public ICollection<ISimulationTest> Tests { get; private set; }
        public Action<ServiceForwarder> SetUp { get; set; }
        public object Fixture { get; set; }
    }
}
