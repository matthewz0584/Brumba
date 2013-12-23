using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Proxy;
using NUnit.Framework;
using Mrse = Microsoft.Robotics.Simulation.Engine;
using MrsePxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.SimulationTester.Tests
{
    [TestFixture]
    public class FixtureInfoCreaterTests
    {
        [NUnit.Framework.Test]
        public void CreateFixtureInfo()
        {
            var fi = new FixtureInfoCreator().CreateFixtureInfo(typeof (TestSimTestFixture1));

            Assert.That(fi.Name, Is.EqualTo("fixture_name"));
            Assert.That(fi.Wip, Is.False);
            Assert.That(fi.Object, Is.Not.Null);
            
            Assert.That(fi.SetUp, Is.Not.Null);

            Assert.That(fi.TestInfos.Count, Is.EqualTo(6));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestSimTestFixture1.TestSimTest1).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestSimTestFixture1.TestSimTest2).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestSimTestFixture1.TestSimTest3).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestSimTestFixture1.TestSimTest4).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestSimTestFixture1.TestSimTest5).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestSimTestFixture1.TestSimTestIfaces).Name));
            
            var testerSvc = new SimulationTesterService();
            fi.SetUp(testerSvc);
            Assert.That((fi.Object as TestSimTestFixture1).TesterService, Is.SameAs(testerSvc));

            Assert.That(new FixtureInfoCreator().CreateFixtureInfo(typeof(TestSimTestFixture2)).Wip);
        }

        [NUnit.Framework.Test]
        public void CreateFixtureInfoWithoutSetUp()
        {
            var fi = new FixtureInfoCreator().CreateFixtureInfo(typeof(TestSimTestFixture2));

            Assert.That(fi.SetUp, Is.Null);
        }

        [NUnit.Framework.Test]
        public void CreateTestInfo()
        {
            var sf = new TestSimTestFixture1();
            var ti = new FixtureInfoCreator().CreateTestInfo(typeof(TestSimTestFixture1.TestSimTest1), sf);

            Assert.That(ti.Name, Is.EqualTo("TestSimTest1"));
            Assert.That(ti.Object, Is.Not.Null);
            Assert.That((ti.Object as TestSimTestFixture1.TestSimTest1).Fixture, Is.SameAs(sf));
            Assert.That(ti.EstimatedTime, Is.EqualTo(2.5f));
            Assert.That(ti.IsProbabilistic);
            
            Assert.That(ti.PrepareEntities, Is.Not.Null);
            Assert.That(ti.Start, Is.Not.Null);
            Assert.That(ti.Test, Is.Not.Null);

            ti.PrepareEntities(new Mrse.VisualEntity {State = {Name = "entity_name"}});
            Assert.That((ti.Object as TestSimTestFixture1.TestSimTest1)._log, Is.EqualTo("prepared entity_name"));
            ti.Start().MoveNext();
            Assert.That((ti.Object as TestSimTestFixture1.TestSimTest1)._log, Is.EqualTo("started"));
            var result = false;
            ti.Test(b => result = b, new[] { new MrsePxy.VisualEntity { State = new EntityState{ Name = "entity_pxy_name" } } }, 1.5f).MoveNext();
            Assert.That((ti.Object as TestSimTestFixture1.TestSimTest1)._log, Is.EqualTo("tested entity_pxy_name" + 1.5f));
            Assert.That(result);

            var ti2 = new FixtureInfoCreator().CreateTestInfo(typeof(TestSimTestFixture1.TestSimTest3), null);
            Assert.That(ti2.PrepareEntities, Is.Null);
        }

        [NUnit.Framework.Test]
        public void CreateDeterministicTestInfo()
        {
            var ti = new FixtureInfoCreator().CreateTestInfo(typeof(TestSimTestFixture1.TestSimTest2), null);

            Assert.That(ti.IsProbabilistic, Is.False);
            Assert.That(ti.PrepareEntities, Is.Null);
        }

        [NUnit.Framework.Test]
        public void CreateTestInfoAnyMembersAllowed()
        {
            var ti2 = new FixtureInfoCreator().CreateTestInfo(typeof(TestSimTestFixture1.TestSimTest2), null);

            Assert.That(ti2.PrepareEntities, Is.Null);
            Assert.That(ti2.Start, Is.Null);
            Assert.That(ti2.Test, Is.Null);

            var ti3 = new FixtureInfoCreator().CreateTestInfo(typeof(TestSimTestFixture1.TestSimTest3), new TestSimTestFixture1());

            Assert.That((ti3.Object as TestSimTestFixture1.TestSimTest3).Fixture, Is.Not.Null);
            Assert.That(ti3.PrepareEntities, Is.Null);
            Assert.That(ti3.Start, Is.Null);
            Assert.That(ti3.Test, Is.Null);

            var ti4 = new FixtureInfoCreator().CreateTestInfo(typeof(TestSimTestFixture1.TestSimTest4), new TestSimTestFixture1());

            Assert.That((ti4.Object as TestSimTestFixture1.TestSimTest4).Fixture, Is.Not.Null);
            Assert.That(ti4.PrepareEntities, Is.Not.Null);
            Assert.That(ti4.Start, Is.Null);
            Assert.That(ti4.Test, Is.Null);

            var ti5 = new FixtureInfoCreator().CreateTestInfo(typeof(TestSimTestFixture1.TestSimTest5), new TestSimTestFixture1());

            Assert.That((ti5.Object as TestSimTestFixture1.TestSimTest5).Fixture, Is.Not.Null);
            Assert.That(ti5.PrepareEntities, Is.Not.Null);
            Assert.That(ti5.Start, Is.Not.Null);
            Assert.That(ti5.Test, Is.Null);
        }

        [NUnit.Framework.Test]
        public void CreateTestInfoInterfaces()
        {
            var ti = new FixtureInfoCreator().CreateTestInfo(typeof(TestSimTestFixture1.TestSimTestIfaces), null);

            Assert.That(ti.PrepareEntities, Is.Not.Null);
            Assert.That(ti.Start, Is.Not.Null);
            Assert.That(ti.Test, Is.Not.Null);

            ti.PrepareEntities(new Mrse.VisualEntity());
            Assert.That((ti.Object as TestSimTestFixture1.TestSimTestIfaces)._log, Is.EqualTo("prepared"));
            ti.Start().MoveNext();
            Assert.That((ti.Object as TestSimTestFixture1.TestSimTestIfaces)._log, Is.EqualTo("started"));
            ti.Test(b => { }, new[] { new MrsePxy.VisualEntity() }, 1.5f).MoveNext();
            Assert.That((ti.Object as TestSimTestFixture1.TestSimTestIfaces)._log, Is.EqualTo("tested"));
        }

        [NUnit.Framework.Test]
        public void CollectFixtures()
        {
            var fixtures = new FixtureInfoCreator().CollectFixtures(Assembly.GetAssembly(typeof(FixtureInfoCreaterTests)));

            Assert.That(fixtures.Count(), Is.EqualTo(2));
            Assert.That(fixtures.Single(f => f.Wip).Name, Is.EqualTo("wip_fixture_name"));
            Assert.That(fixtures.Single(f => !f.Wip).Name, Is.EqualTo("fixture_name"));
        }
    }

    [SimTestFixture("fixture_name")]
    public class TestSimTestFixture1
    {
        public SimulationTesterService TesterService { get; private set; }

        [SetUp]
        public void SetUp(SimulationTesterService testerService)
        {
            TesterService = testerService;
        }

        [SimTest(2.5f)]
        public class TestSimTest1 : IStart
        {
            public string _log = "";

            [Fixture]
            public TestSimTestFixture1 Fixture { get; set; }

            [PrepareEntities]
            public void PrepareEntities(Mrse.VisualEntity entity)
            {
                _log = "prepared " + entity.State.Name;
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                _log = "started";
                yield break;
            }

            [Test]
            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<MrsePxy.VisualEntity> simStateEntities, double elapsedTime)
            {
                _log = "tested " + simStateEntities.First().State.Name + elapsedTime;
                @return(true);
                yield break; 
            }
        }

        [SimTest(3, IsProbabilistic = false)]
        public class TestSimTest2
        {
        }

        [SimTest(2.5f)]
        public class TestSimTest3
        {
            [Fixture]
            public TestSimTestFixture1 Fixture { get; set; }
        }

        [SimTest(2.5f)]
        public class TestSimTest4
        {
            [Fixture]
            public TestSimTestFixture1 Fixture { get; set; }

            [PrepareEntities]
            public void PrepareEntities(Mrse.VisualEntity entity)
            {
            }
        }

        [SimTest(2.5f)]
        public class TestSimTest5
        {
            [Fixture]
            public TestSimTestFixture1 Fixture { get; set; }

            [PrepareEntities]
            public void PrepareEntities(Mrse.VisualEntity entity)
            {
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield break;
            }
        }

        [SimTest(2.5f)]
        public class TestSimTestIfaces : IPrepareEntities, IStart, ITest
        {
            public string _log = "";
            
            public void PrepareEntities(Mrse.VisualEntity entity)
            {
                _log = "prepared";
            }

            public IEnumerator<ITask> Start()
            {
                _log = "started";
                yield break;
            }

            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<MrsePxy.VisualEntity> simStateEntities,
                                           double elapsedTime)
            {
                _log = "tested";
                yield break;
            }
        }
    }

    [SimTestFixture("wip_fixture_name", Wip = true)]
    public class TestSimTestFixture2
    {
    }
}
