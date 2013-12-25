using System;
using System.Collections.Generic;

namespace Brumba.SimulationTester
{
	class SimulationTesterPresenterConsole
	{
        public void Setup(SimulationTesterService tester)
		{
			tester.OnFixtureStarted += fi => Console.WriteLine("Fixture {0}", fi.Object.GetType().Name);
			tester.OnTestStarted += t => Console.Write("{0,20} ", t.Name);
			tester.OnTestEnded += OnTestEnded;
			tester.OnTestTryEnded += OnTestTryEnded;
			tester.OnStarted += () => Console.WriteLine();
			tester.OnEnded += OnEnded;
		}

        void OnTestTryEnded(SimulationTestInfo t, bool r)
	    {
	        if (t.IsProbabilistic) WriteColored(r ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed, r ? "." : "x");
	    }

        void OnTestEnded(SimulationTestInfo t, float r)
	    {
	        WriteTestResult(t, r);
	        Console.WriteLine();
	    }

	    void OnEnded(Dictionary<SimulationTestInfo, float> testResults)
		{
			Console.WriteLine();
			Console.WriteLine("Total tests {0}:", testResults.Count);
			foreach (var tr in testResults)
                WriteTestResult(tr.Key, tr.Value);
		}

        private static void WriteTestResult(SimulationTestInfo ti, float r)
        {
            if (ti.IsProbabilistic)
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