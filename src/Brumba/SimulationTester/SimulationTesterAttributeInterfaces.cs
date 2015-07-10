using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Mrse = Microsoft.Robotics.Simulation.Engine;
using MrsePxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.SimulationTester
{
    public interface ISetUp
    {
        void SetUp(SimulationTesterService testerService);
    }

    public interface IFixture<T>
    {
        T Fixture { get; set; }
    }

    public interface IPrepare
    {
        void Prepare(IEnumerable<Mrse.VisualEntity> simEntities);
    }

    public interface IStart
    {
        IEnumerator<ITask> Start();
    }

    public interface ITest
    {
        IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<MrsePxy.VisualEntity> simEntities, double elapsedTime);
    }
}


