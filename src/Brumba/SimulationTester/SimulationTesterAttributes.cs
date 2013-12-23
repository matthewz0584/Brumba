using System;

namespace Brumba.SimulationTester
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SimTestFixtureAttribute : Attribute
    {
        public string Name { get; set; }
        public bool Wip { get; set; }
        public bool Ignore { get; set; }

        public SimTestFixtureAttribute(string name, bool ignore = false, bool wip = false)
        {
            Name = name;
            Ignore = ignore;
            Wip = wip;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SimTestAttribute : Attribute
    {
        public float EstimatedTime { get; set; }

        public bool IsProbabilistic { get; set; }

        public SimTestAttribute(float estimatedTime)
        {
            EstimatedTime = estimatedTime;
            IsProbabilistic = true;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SetUpAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PrepareEntitiesAttribute : Attribute
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