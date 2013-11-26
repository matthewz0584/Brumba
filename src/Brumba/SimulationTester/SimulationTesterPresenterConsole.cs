using System;
using System.Collections.Generic;

namespace Brumba.SimulationTester
{
	class SimulationTesterPresenterConsole
	{
        public void Setup(SimulationTesterService tester)
		{
			tester.OnFixtureStarted += fi => Console.WriteLine("Fixture {0}", fi.Fixture.GetType().Name);
			tester.OnTestStarted += t => Console.Write("{0,20} ", t.GetType().Name);
			tester.OnTestEnded += OnTestEnded;
			tester.OnTestTryEnded += OnTestTryEnded;
			tester.OnStarted += () => Console.WriteLine();
			tester.OnEnded += OnEnded;
		}

	    void OnTestTryEnded(ISimulationTest t, bool r)
	    {
	        if (t.IsProbabilistic) WriteColored(r ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed, r ? "." : "x");
	    }

	    void OnTestEnded(ISimulationTest t, float r)
	    {
	        WriteTestResult(t, r);
	        Console.WriteLine();
	    }

	    void OnEnded(Dictionary<ISimulationTest, float> testResults)
		{
			Console.WriteLine();
			foreach (var tr in testResults)
                WriteTestResult(tr.Key, tr.Value);
		}

        private static void WriteTestResult(ISimulationTest t, float r)
        {
            if (t.IsProbabilistic)
                WriteColored(r >= 0.8 ? ConsoleColor.Green : ConsoleColor.Red, " {0:P0}", r);
            else
                WriteColored(r == 1 ? ConsoleColor.Green : ConsoleColor.Red, " {0}", r == 1 ? 'V' : 'X');
        }

		static void WriteColored(ConsoleColor color, string text, params object[] objs)
		{
			Console.ForegroundColor = color;
			Console.Write(text, objs);
			Console.ResetColor();
		}
	}
}