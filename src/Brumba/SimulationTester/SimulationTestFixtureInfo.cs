using System;
using System.Collections.Generic;
using Brumba.Utils;

namespace Brumba.SimulationTester
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
        public Action<DsspServiceExposing> SetUp { get; set; }
        public object Fixture { get; set; }
    }
}
