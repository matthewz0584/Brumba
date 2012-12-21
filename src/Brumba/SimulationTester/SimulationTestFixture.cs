using System.Collections.Generic;

namespace Brumba.Simulation.SimulationTester
{
    public interface ISimulationTestFixture
    {
        string EnvironmentXmlFile { get; }
        IEnumerable<ISimulationTest> Tests { get; }
    }

    public class SimulationTestFixture : ISimulationTestFixture
    {
    	readonly IEnumerable<SingleVehicleTest> _tests;
    	readonly string _environmentXmlFile;

        protected SimulationTestFixture(IEnumerable<SingleVehicleTest> tests, string environmentXmlFile)
        {
            _tests = tests;
            foreach (var test in _tests)
                test.Fixture = this;

            _environmentXmlFile = environmentXmlFile;
        }

        public IEnumerable<ISimulationTest> Tests { get { return _tests; } }
        public string EnvironmentXmlFile { get { return _environmentXmlFile; } }
    }
}
