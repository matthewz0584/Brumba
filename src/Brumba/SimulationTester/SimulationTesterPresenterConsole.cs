using System;
using System.Collections.Generic;

namespace Brumba.Simulation.SimulationTester
{
	class SimulationTesterPresenterConsole
	{
        public void Setup(SimulationTesterService tester)
		{
			tester.OnFixtureStarted += f => Console.WriteLine("Fixture {0}", f.GetType().Name);
			tester.OnTestStarted += t => Console.Write("{0,20} ", t.GetType().Name);
			tester.OnTestEnded += OnTestEnded;
			tester.OnTestTryEnded += (t, r) => WriteColored(r ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed, r ? "." : "x");
			tester.OnStarted += () => Console.WriteLine();
			tester.OnEnded += OnEnded;
		}

		void OnTestEnded(ISimulationTest t, float r)
		{
			WriteColored(r >= 0.8 ? ConsoleColor.Green : ConsoleColor.Red, " {0:P0}", r);
			Console.WriteLine();
		}

		void OnEnded(Dictionary<ISimulationTest, float> testResults)
		{
			Console.WriteLine();
			foreach (var r in testResults.Values)
				WriteColored(r >= 0.8 ? ConsoleColor.Green : ConsoleColor.Red, " {0:P0}", r);
		}

		static void WriteColored(ConsoleColor color, string text, params object[] objs)
		{
			Console.ForegroundColor = color;
			Console.Write(text, objs);
			Console.ResetColor();
		}
	}
}