using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Ccr.Core;

namespace Brumba.SimulationTester
{
    public class FixtureInfoCreator
    {
        public IEnumerable<SimulationTestFixtureInfo> CollectFixtures(Assembly assembly)
        {
            return assembly.GetTypes().
                Where(t => t.GetCustomAttributes(false).Any(a => a is SimTestFixtureAttribute && !(a as SimTestFixtureAttribute).Ignore)).
                Select(new FixtureInfoCreator().CreateFixtureInfo);
        }

        public SimulationTestFixtureInfo CreateFixtureInfo(Type fixtureType)
        {
            var fixtureInfo = new SimulationTestFixtureInfo();

            fixtureInfo.Object = Activator.CreateInstance(fixtureType);

            var simTestFixtureAttribute = fixtureType.GetCustomAttributes(false).OfType<SimTestFixtureAttribute>().Single();
            fixtureInfo.Name = simTestFixtureAttribute.Name;
            fixtureInfo.Wip = simTestFixtureAttribute.Wip;

            var setupMethod = fixtureType.GetMethods().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is SetUpAttribute));
            if (setupMethod != null)
                fixtureInfo.SetUp = sf =>
                    setupMethod.Invoke(fixtureInfo.Object, new object[] { sf });

            var testsToCreate = fixtureType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                                           .Where(t => t.GetCustomAttributes(false).Any(a => a is SimTestAttribute));
            foreach (var testType in testsToCreate)
                fixtureInfo.TestInfos.Add(CreateTestInfo(testType, fixtureInfo.Object));
            
            return fixtureInfo;
        }

        public SimulationTestInfo CreateTestInfo(Type testType, object fixtureObject)
        {
            var sti = new SimulationTestInfo {Name = testType.Name};
            sti.Object = Activator.CreateInstance(testType);
            sti.EstimatedTime = testType.GetCustomAttributes(false).OfType<SimTestAttribute>().Single().EstimatedTime;
            sti.IsProbabilistic = testType.GetCustomAttributes(false).OfType<SimTestAttribute>().Single().IsProbabilistic;

            var fixtureProperty = testType.GetProperties().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is FixtureAttribute));
            if (fixtureProperty != null)
                fixtureProperty.SetValue(sti.Object, fixtureObject, null);

            var prepareEntitiesMethod = testType.GetMethods().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is PrepareEntitiesAttribute));
            if (prepareEntitiesMethod != null)
                sti.PrepareEntities = entity =>
                    prepareEntitiesMethod.Invoke(sti.Object, new object[] { entity });
            else if (sti.Object is IPrepareEntities)
                sti.PrepareEntities = (sti.Object as IPrepareEntities).PrepareEntities;

            var startMethod = testType.GetMethods().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is StartAttribute));
            if (startMethod != null)
                sti.Start = () =>
                    startMethod.Invoke(sti.Object, new object[] {}) as IEnumerator<ITask>;
            else if (sti.Object is IStart)
                sti.Start = (sti.Object as IStart).Start;

            var testMethod = testType.GetMethods().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is TestAttribute));
            if (testMethod != null)
                sti.Test = (@return, entityFromSim, elapsedTime) => 
                    testMethod.Invoke(sti.Object, new object[] { @return, entityFromSim, elapsedTime }) as IEnumerator<ITask>;
            else if (sti.Object is ITest)
                sti.Test = (sti.Object as ITest).Test;

            return sti;
        }
    }
}