using System;

namespace Brumba.SimulationTestRunner
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SimTestFixtureAttribute : Attribute
    {
        public string Name { get; set; }
        public bool Wip { get; set; }
        public bool Ignore { get; set; }
		public float PhysicsTimeStep { get; set; }

        public SimTestFixtureAttribute(string name, bool ignore = false, bool wip = false, float physicsTimeStep = 0.01f)
        {
            Name = name;
            Ignore = ignore;
            Wip = wip;
	        PhysicsTimeStep = physicsTimeStep;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SimTestAttribute : Attribute
    {
        public float EstimatedTime { get; set; }

        public bool IsProbabilistic { get; set; }

		public bool TestAllEntities { get; set; }

        public SimTestAttribute(float estimatedTime)
        {
            EstimatedTime = estimatedTime;
            IsProbabilistic = true;
	        TestAllEntities = false;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SetUpAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PrepareAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class StartAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FixtureAttribute : Attribute
    {
    }
}