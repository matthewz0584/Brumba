using System;
using System.Collections.Generic;

namespace Brumba.SimulationTester
{
    public class SimulationTestFixtureInfo
    {
        public SimulationTestFixtureInfo()
        {
            TestInfos = new List<SimulationTestInfo>();
            Name = "";
        }

        public string Name { get; set; }
        public bool Wip { get; set; }
        public ICollection<SimulationTestInfo> TestInfos { get; private set; }
        public Action<SimulationTesterService> SetUp { get; set; }
        public object Object { get; set; }
    }
}
