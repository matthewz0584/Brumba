using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Ccr.Core;
using Mrse = Microsoft.Robotics.Simulation.Engine;
using MrsePxy = Microsoft.Robotics.Simulation.Engine.Proxy;

namespace Brumba.SimulationTestRunner
{
    public class FixtureInfoCreator
    {
        public List<SimulationTestFixtureInfo> CollectFixtures(Assembly assembly, bool ignoreFails)
        {
            return assembly.GetTypes().
                Where(t => t.GetCustomAttributes(false).Any(a => a is SimTestFixtureAttribute && !(a as SimTestFixtureAttribute).Ignore)).
				Select(fixtureType =>
					{
						try
						{
							return new FixtureInfoCreator().CreateFixtureInfo(fixtureType);
						}
						catch (FixtureInfoCreaterException e)
						{
							if (ignoreFails)
								return null;
							throw new FixtureInfoCreaterException(
								string.Format("{0} test fixture has some malformed tests",
											  fixtureType.GetCustomAttributes(false).OfType<SimTestFixtureAttribute>().Single().Name), e);
						}
					}).
				Where(fi => fi != null).ToList();
        }

        public SimulationTestFixtureInfo CreateFixtureInfo(Type fixtureType)
        {
            var fixtureInfo = new SimulationTestFixtureInfo();

            fixtureInfo.Object = Activator.CreateInstance(fixtureType);

            var simTestFixtureAttribute = fixtureType.GetCustomAttributes(false).OfType<SimTestFixtureAttribute>().Single();
            fixtureInfo.Name = simTestFixtureAttribute.Name;
            fixtureInfo.Wip = simTestFixtureAttribute.Wip;
	        fixtureInfo.PhysicsTimeStep = simTestFixtureAttribute.PhysicsTimeStep;

            var setupMethod = fixtureType.GetMethods().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is SetUpAttribute));
            if (setupMethod != null)
            {
                CheckMethodInfo(setupMethod, typeof(ISetUp).GetMethods().Single(), fixtureInfo.Name, typeof(SetUpAttribute).Name);
                fixtureInfo.SetUp = sf => setupMethod.Invoke(fixtureInfo.Object, new object[] {sf});
            }
            else if (fixtureInfo.Object is ISetUp)
                fixtureInfo.SetUp= (fixtureInfo.Object as ISetUp).SetUp;

            var testsToCreate = fixtureType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                                           .Where(t => t.GetCustomAttributes(false).Any(a => a is SimTestAttribute));
            foreach (var testType in testsToCreate)
                fixtureInfo.TestInfos.Add(CreateTestInfo(testType, fixtureInfo.Object));
            
            return fixtureInfo;
        }

        public SimulationTestInfo CreateTestInfo(Type testType, object fixtureObject)
        {
            var sti = new SimulationTestInfo
            {
	            Name = testType.Name,
	            Object = Activator.CreateInstance(testType),
	            EstimatedTime = testType.GetCustomAttributes(false).OfType<SimTestAttribute>().Single().EstimatedTime,
	            IsProbabilistic = testType.GetCustomAttributes(false).OfType<SimTestAttribute>().Single().IsProbabilistic,
	            TestAllEntities = testType.GetCustomAttributes(false).OfType<SimTestAttribute>().Single().TestAllEntities
            };

	        var fixtureProperty = testType.GetProperties().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is FixtureAttribute));
            if (fixtureProperty != null)
            {
                fixtureProperty.SetValue(sti.Object, fixtureObject, null);
            }
            else if (testType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof (IFixture<>)))
            {
                testType.GetInterfaces().Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof (IFixture<>)).
                    GetProperties().Single().SetValue(sti.Object, fixtureObject, null);
            }

            var prepareEntitiesMethod = testType.GetMethods().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is PrepareAttribute));
			if (prepareEntitiesMethod != null)
			{
				CheckMethodInfo(prepareEntitiesMethod, typeof(IPrepare).GetMethods().Single(), sti.Name, typeof(PrepareAttribute).Name);
				sti.Prepare = entity => prepareEntitiesMethod.Invoke(sti.Object, new object[] {entity});
			}
			else if (sti.Object is IPrepare)
				sti.Prepare = (sti.Object as IPrepare).Prepare;

            var startMethod = testType.GetMethods().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is StartAttribute));
			if (startMethod != null)
			{
				CheckMethodInfo(startMethod, typeof(IStart).GetMethods().Single(), sti.Name, typeof(StartAttribute).Name);
				sti.Start = () => startMethod.Invoke(sti.Object, new object[] {}) as IEnumerator<ITask>;
			}
			else if (sti.Object is IStart)
				sti.Start = (sti.Object as IStart).Start;

            var testMethod = testType.GetMethods().SingleOrDefault(mi => mi.GetCustomAttributes(false).Any(a => a is TestAttribute));
			if (testMethod != null)
			{
				CheckMethodInfo(testMethod, typeof(ITest).GetMethods().Single(), sti.Name, typeof(TestAttribute).Name);
				sti.Test = (@return, entityFromSim, elapsedTime) => testMethod.Invoke(sti.Object, new object[] {@return, entityFromSim, elapsedTime}) as IEnumerator<ITask>;
			}
			else if (sti.Object is ITest)
				sti.Test = (sti.Object as ITest).Test;

            return sti;
        }

		static void CheckMethodInfo(MethodInfo candidatePrototype, MethodInfo correctPrototype, string testName, string attTypeName)
	    {
			if (candidatePrototype.ReturnType != correctPrototype.ReturnType)
				throw new FixtureInfoCreaterException(string.Format("{0}.{1} method has wrong return type for {2}", testName, candidatePrototype.Name, attTypeName));
			if (!candidatePrototype.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(
				 correctPrototype.GetParameters().Select(pi => pi.ParameterType)))
				throw new FixtureInfoCreaterException(string.Format("{0}.{1} method has wrong parameters for {2}", testName, candidatePrototype.Name, attTypeName));
	    }
    }

	public class FixtureInfoCreaterException : Exception
	{
		public FixtureInfoCreaterException(string message) : base(message)
		{}

		public FixtureInfoCreaterException(string message, Exception inner) : base(message, inner)
		{}
	}
}