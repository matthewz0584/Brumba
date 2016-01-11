using System.Diagnostics;
using Microsoft.Dss.Diagnostics;

namespace Brumba.SimulationTestRunner
{
    [CategoryNamespace("http://brumba.ru/contracts/2012/11/simulationtester.html")]
    public enum SimulationTestRunnerLogCategory
    {
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        TestStarted,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Try")]
        TestEnvironmentRestored,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Try")]
        TestManifestRestarted,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Try")]
        TestFixtureSetUp,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Try")]
        TestTryStarted,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Try")]
        [CategoryArgument(3, "Estimated time")]
        TestEstimatedTimeElapsed,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Try")]
        TestTesteeEntitiesDeserialized,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Try")]
        [CategoryArgument(3, "Success")]
        TestResultsAssessed,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Try")]
        TestServicesDropped,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Result")]
        TestRunningResult,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        [CategoryArgument(1, "Test")]
        [CategoryArgument(2, "Result")]
        TestFinished,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        FixtureStarted,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        FixtureStaticEnvironmentRestored,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        FixtureServicesDropped,
        [OperationalCategory(TraceLevel.Info, LogCategoryFlags.None)]
        [CategoryArgument(0, "Fixture")]
        FixtureFinished
    }
}