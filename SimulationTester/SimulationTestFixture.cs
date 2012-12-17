using System.Collections.Generic;

namespace Brumba.Simulation.SimulationTester
{
    public interface ISimulationTestFixture
    {
        string EnvironmentXmlFile { get; }
        IEnumerable<string> ObjectsToRestore { get; }
        IEnumerable<ISimulationTest> Tests { get; }
    }

    public class SimulationTestFixture : ISimulationTestFixture
    {
    	readonly IEnumerable<SimulationTest> _tests;
    	readonly string _environmentXmlFile;
    	readonly IEnumerable<string> _objectsToRestore;

        protected SimulationTestFixture(IEnumerable<SimulationTest> tests, string environmentXmlFile, IEnumerable<string> objectsToRestore)
        {
            _tests = tests;
            foreach (var test in _tests)
                test.Fixture = this;

            _environmentXmlFile = environmentXmlFile;
            _objectsToRestore = objectsToRestore;
        }

        public IEnumerable<ISimulationTest> Tests { get { return _tests; } }
        public string EnvironmentXmlFile { get { return _environmentXmlFile; } }
        public IEnumerable<string> ObjectsToRestore { get { return _objectsToRestore; } }
    }
}
