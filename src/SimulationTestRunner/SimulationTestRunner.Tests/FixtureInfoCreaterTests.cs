﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Simulation.Proxy;
using NUnit.Framework;
using Mrse = Microsoft.Robotics.Simulation.Engine;
using MrsePxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.SimulationTestRunner.Tests
{
    [TestFixture]
    public class FixtureInfoCreaterTests
    {
        [NUnit.Framework.Test]
        public void CreateFixtureInfo()
        {
            var fi = new FixtureInfoCreator().CreateFixtureInfo(typeof (TestFixture));

            Assert.That(fi.Name, Is.EqualTo("fixture_name"));
            Assert.That(fi.Wip, Is.False);
            Assert.That(fi.Object, Is.Not.Null);
            Assert.That(fi.SetUp, Is.Not.Null);
			Assert.That(fi.PhysicsTimeStep, Is.EqualTo(0.001f));

            Assert.That(fi.TestInfos.Count, Is.EqualTo(6));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestFixture.TestWithAttributes).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestFixture.TestEmpty).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestFixture.TestWithFixtureAtt).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestFixture.TestWithFixtureAndPrepareAtt).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestFixture.TestWithFixtureAndPrepareAndStartAtt).Name));
            Assert.That(fi.TestInfos.Any(ti => ti.Name == typeof(TestFixture.TestWithIfaces).Name));
            
            var testerSvc = new SimulationTestRunnerService();
            fi.SetUp(testerSvc);
            Assert.That((fi.Object as TestFixture).TesterService, Is.SameAs(testerSvc));

            Assert.That(new FixtureInfoCreator().CreateFixtureInfo(typeof(WipFixture)).Wip);
        }

        [NUnit.Framework.Test]
        public void CreateFixtureInfoWithoutSetUp()
        {
            var fi = new FixtureInfoCreator().CreateFixtureInfo(typeof(WipFixture));

            Assert.That(fi.SetUp, Is.Null);
        }

        [NUnit.Framework.Test]
        public void CreateFixtureInfoInterfaceSetup()
        {
            var fi = new FixtureInfoCreator().CreateFixtureInfo(typeof(InterfaceSetupFixture));

            Assert.That(fi.SetUp, Is.Not.Null);

            var testerSvc = new SimulationTestRunnerService();
            fi.SetUp(testerSvc);
            Assert.That((fi.Object as InterfaceSetupFixture).TesterService, Is.SameAs(testerSvc));
        }

        [NUnit.Framework.Test]
        public void CreateTestInfoFromAttributes()
        {
            var tf = new TestFixture();
            var ti = new FixtureInfoCreator().CreateTestInfo(typeof(TestFixture.TestWithAttributes), tf);

            Assert.That(ti.Name, Is.EqualTo("TestWithAttributes"));
            Assert.That(ti.Object, Is.Not.Null);
            Assert.That((ti.Object as TestFixture.TestWithAttributes).Fixture, Is.SameAs(tf));
            Assert.That(ti.EstimatedTime, Is.EqualTo(2.5f));
            Assert.That(ti.IsProbabilistic);
            
            Assert.That(ti.Prepare, Is.Not.Null);
            Assert.That(ti.Start, Is.Not.Null);
            Assert.That(ti.Test, Is.Not.Null);

            ti.Prepare(new [] {new Mrse.VisualEntity {State = {Name = "entity_name"}}});
            Assert.That((ti.Object as TestFixture.TestWithAttributes)._log, Is.EqualTo("prepared entity_name"));
            ti.Start().MoveNext();
            Assert.That((ti.Object as TestFixture.TestWithAttributes)._log, Is.EqualTo("started"));
            var result = false;
            ti.Test(b => result = b, new[] { new MrsePxy.VisualEntity { State = new EntityState{ Name = "entity_pxy_name" } } }, 1.5f).MoveNext();
            Assert.That((ti.Object as TestFixture.TestWithAttributes)._log, Is.EqualTo("tested entity_pxy_name" + 1.5f));
            Assert.That(result);

            var ti2 = new FixtureInfoCreator().CreateTestInfo(typeof(TestFixture.TestWithFixtureAtt), null);
            Assert.That(ti2.Prepare, Is.Null);
        }

        [NUnit.Framework.Test]
        public void CreateDeterministicAndTestAllTestInfo()
        {
            var ti = new FixtureInfoCreator().CreateTestInfo(typeof(TestFixture.TestEmpty), null);

            Assert.That(ti.IsProbabilistic, Is.False);
			Assert.That(ti.TestAllEntities);
            Assert.That(ti.Prepare, Is.Null);
        }

        [NUnit.Framework.Test]
        public void CreateTestInfoFromAttributesAnyAllowed()
        {
            var ti2 = new FixtureInfoCreator().CreateTestInfo(typeof(TestFixture.TestEmpty), null);

            Assert.That(ti2.Prepare, Is.Null);
            Assert.That(ti2.Start, Is.Null);
            Assert.That(ti2.Test, Is.Null);

            var ti3 = new FixtureInfoCreator().CreateTestInfo(typeof(TestFixture.TestWithFixtureAtt), new TestFixture());

            Assert.That((ti3.Object as TestFixture.TestWithFixtureAtt).Fixture, Is.Not.Null);
            Assert.That(ti3.Prepare, Is.Null);
            Assert.That(ti3.Start, Is.Null);
            Assert.That(ti3.Test, Is.Null);

            var ti4 = new FixtureInfoCreator().CreateTestInfo(typeof(TestFixture.TestWithFixtureAndPrepareAtt), new TestFixture());

            Assert.That((ti4.Object as TestFixture.TestWithFixtureAndPrepareAtt).Fixture, Is.Not.Null);
            Assert.That(ti4.Prepare, Is.Not.Null);
            Assert.That(ti4.Start, Is.Null);
            Assert.That(ti4.Test, Is.Null);

            var ti5 = new FixtureInfoCreator().CreateTestInfo(typeof(TestFixture.TestWithFixtureAndPrepareAndStartAtt), new TestFixture());

            Assert.That((ti5.Object as TestFixture.TestWithFixtureAndPrepareAndStartAtt).Fixture, Is.Not.Null);
            Assert.That(ti5.Prepare, Is.Not.Null);
            Assert.That(ti5.Start, Is.Not.Null);
            Assert.That(ti5.Test, Is.Null);
        }

        [NUnit.Framework.Test]
        public void CreateTestInfoFromInterfaces()
        {
            var tf = new TestFixture();
            var ti = new FixtureInfoCreator().CreateTestInfo(typeof(TestFixture.TestWithIfaces), tf);

            Assert.That((ti.Object as TestFixture.TestWithIfaces).Fixture, Is.SameAs(tf));
            Assert.That(ti.Prepare, Is.Not.Null);
            Assert.That(ti.Start, Is.Not.Null);
            Assert.That(ti.Test, Is.Not.Null);

            ti.Prepare(new [] {new Mrse.VisualEntity()});
            Assert.That((ti.Object as TestFixture.TestWithIfaces)._log, Is.EqualTo("prepared"));
            ti.Start().MoveNext();
            Assert.That((ti.Object as TestFixture.TestWithIfaces)._log, Is.EqualTo("started"));
            ti.Test(b => { }, new[] { new MrsePxy.VisualEntity() }, 1.5f).MoveNext();
            Assert.That((ti.Object as TestFixture.TestWithIfaces)._log, Is.EqualTo("tested"));
        }

        [NUnit.Framework.Test]
        public void CollectFixtures()
        {
            var fixtures = new FixtureInfoCreator().CollectFixtures(Assembly.GetAssembly(typeof(FixtureInfoCreaterTests)), true);

            Assert.That(fixtures.Count(), Is.EqualTo(3));
            Assert.That(fixtures.Select(fi => fi.Name), Is.EquivalentTo(new[] { "wip_fixture", "fixture_name", "interface_setup_fixture" }));
        }

		[NUnit.Framework.Test]
		[ExpectedException(typeof(FixtureInfoCreaterException), ExpectedMessage = "TestWithWrongPreparePrototype.Prepare method has wrong return type for PrepareAttribute")]
		public void CreateTestInfoFromAttributesWrongPrepare()
		{
			new FixtureInfoCreator().CreateTestInfo(typeof(WrongFixture.TestWithWrongPreparePrototype), null);
		}

		[NUnit.Framework.Test]
		[ExpectedException(typeof(FixtureInfoCreaterException), ExpectedMessage = "TestWithWrongStartPrototype.Start method has wrong parameters for StartAttribute")]
		public void CreateTestInfoFromAttributesWrongStart()
		{
			new FixtureInfoCreator().CreateTestInfo(typeof(WrongFixture.TestWithWrongStartPrototype), null);
		}

		[NUnit.Framework.Test]
		[ExpectedException(typeof(FixtureInfoCreaterException), ExpectedMessage = "TestWithWrongTestPrototype.Test method has wrong parameters for TestAttribute")]
		public void CreateTestInfoFromAttributesWrongTest()
		{
			new FixtureInfoCreator().CreateTestInfo(typeof(WrongFixture.TestWithWrongTestPrototype), null);
		}

		[NUnit.Framework.Test]
		[ExpectedException(typeof(FixtureInfoCreaterException), ExpectedMessage = "wrong_fixture test fixture has some malformed tests")]
		public void CollectFixturesIgnoreFails()
		{
			new FixtureInfoCreator().CollectFixtures(Assembly.GetAssembly(typeof(FixtureInfoCreaterTests)), false);
		}
	}

	[SimTestFixture("fixture_name", PhysicsTimeStep = 0.001f)]
    public class TestFixture
    {
        public SimulationTestRunnerService TesterService { get; private set; }

        [SetUp]
        public void SetUp(SimulationTestRunnerService testerService)
        {
            TesterService = testerService;
        }

        [SimTest(2.5f)]
        public class TestWithAttributes
        {
            public string _log = "";

            [Fixture]
            public TestFixture Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(IEnumerable<Mrse.VisualEntity> entities)
            {
                _log = "prepared " + entities.Single().State.Name;
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

        [SimTest(3, IsProbabilistic = false, TestAllEntities = true)]
        public class TestEmpty
        {
        }

        [SimTest(2.5f)]
        public class TestWithFixtureAtt
        {
            [Fixture]
            public TestFixture Fixture { get; set; }
        }

        [SimTest(2.5f)]
        public class TestWithFixtureAndPrepareAtt
        {
            [Fixture]
            public TestFixture Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(IEnumerable<Mrse.VisualEntity> entities)
            {
            }
        }

        [SimTest(2.5f)]
        public class TestWithFixtureAndPrepareAndStartAtt
        {
            [Fixture]
            public TestFixture Fixture { get; set; }

            [Prepare]
            public void PrepareEntities(IEnumerable<Mrse.VisualEntity> entities)
            {
            }

            [Start]
            public IEnumerator<ITask> Start()
            {
                yield break;
            }
        }

        [SimTest(2.5f)]
        public class TestWithIfaces : IFixture<TestFixture>, IPrepare, IStart, ITest
        {
            public string _log = "";

            public TestFixture Fixture { get; set; }
            
            public void Prepare(IEnumerable<Mrse.VisualEntity> simEntities)
            {
                _log = "prepared";
            }

            public IEnumerator<ITask> Start()
            {
                _log = "started";
                yield break;
            }

            public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<MrsePxy.VisualEntity> simEntities, double elapsedTime)
            {
                _log = "tested";
                yield break;
            }
        }
    }

    [SimTestFixture("wip_fixture", Wip = true)]
    public class WipFixture
    {
    }

	[SimTestFixture("wrong_fixture", Wip = true)]
	public class WrongFixture
	{
		[SimTest(2.5f)]
		public class TestWithWrongPreparePrototype
		{
			[Prepare]
			public string Prepare(IEnumerable<Mrse.VisualEntity> entities)
			{
				return "";
			}
		}

		[SimTest(2.5f)]
		public class TestWithWrongStartPrototype
		{
			[Start]
			public IEnumerator<ITask> Start(int wrongParameter)
			{
				yield break;
			}
		}

		[SimTest(2.5f)]
		public class TestWithWrongTestPrototype
		{
			[Test]
			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<MrsePxy.VisualEntity> simStateEntities, string elapsedTime)
			{
				yield break;
			}
		}		
	}

    [SimTestFixture("interface_setup_fixture")]
    public class InterfaceSetupFixture : ISetUp
    {
        public SimulationTestRunnerService TesterService { get; private set; }

        public void SetUp(SimulationTestRunnerService testerService)
        {
            TesterService = testerService;
        }
    }
}
