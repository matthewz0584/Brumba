using System.Collections.Generic;

namespace Brumba.Simulation.SimulationTester
{
    public interface ISimulationTestFixture
    {
        string EnvironmentXmlFile { get; }
        IEnumerable<ISimulationTest> Tests { get; }
        void SetUpServicePorts();
    }

    public abstract class SimulationTestFixture : ISimulationTestFixture
    {
        public const string MANIFEST_EXTENSION = "manifest.xml";
        public const string ENVIRONMENT_EXTENSION = "xml";

        readonly IEnumerable<ISimulationTest> _tests;
    	readonly string _environmentXmlFile;
        readonly ServiceForwarder _serviceForwarder;

        protected SimulationTestFixture(IEnumerable<ISimulationTest> tests, string environmentXmlFile, ServiceForwarder serviceForwarder)
        {
            _serviceForwarder = serviceForwarder;
            _tests = tests;
            foreach (var test in _tests)
                test.Fixture = this;

            _environmentXmlFile = environmentXmlFile;
        }

        public IEnumerable<ISimulationTest> Tests { get { return _tests; } }
        public string EnvironmentXmlFile { get { return _environmentXmlFile; } }
        public void SetUpServicePorts() { SetUpServicePorts(_serviceForwarder); }

        protected abstract void SetUpServicePorts(ServiceForwarder serviceForwarder);
    }
}
