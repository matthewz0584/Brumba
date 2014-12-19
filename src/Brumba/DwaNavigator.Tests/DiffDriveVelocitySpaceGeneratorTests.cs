using System;
using System.Linq;
using Brumba.Utils;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class DiffDriveVelocitySpaceGeneratorTests
    {
        [Test]
        public void WheelToRobotKinematics()
        {
            var ddvsg = new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1.5d, 3d, 1, 1, 1);

            Assert.That(ddvsg.WheelsToRobotKinematics(new Vector2(2, 1)), Is.EqualTo(new Velocity(1.5d / 2 * (2 + 1), 1.5d / 3d * (1 - 2))));
        }

        [Test]
        public void PredictWheelVelocititesParams()
        {
            var ddvsg = new DiffDriveVelocitySpaceGenerator(1, 1, 10, 2, 1, 1, 0, 1);

            Assert.That(ddvsg.PredictWheelVelocities(new Vector2(1.3f, 1.5f), new Vector2(1, 1), 0), Is.EqualTo(new Vector2(1.3f, 1.5f)));
            
            Assert.That(ddvsg.PredictWheelVelocities(new Vector2(1.3f, 1.5f), new Vector2(), 1000).EqualsWithError(new Vector2(), 1e-7));

            Assert.That(ddvsg.PredictWheelVelocities(new Vector2(), new Vector2(1, -1), 1000), Is.EqualTo(new Vector2(10 / 2, -10 / 2)));
            Assert.That(ddvsg.PredictWheelVelocities(new Vector2(), new Vector2(-1, 1), 1000), Is.EqualTo(new Vector2(-10 / 2, 10 / 2)));

            Assert.That(ddvsg.PredictWheelVelocities(new Vector2(), new Vector2(0.5f, -0.5f), 1000), Is.EqualTo(new Vector2(5f / 2, -5f / 2)));
            Assert.That(ddvsg.PredictWheelVelocities(new Vector2(), new Vector2(-0.5f, 0.5f), 1000), Is.EqualTo(new Vector2(-5f / 2, 5f / 2)));

            Assert.That(ddvsg.PredictWheelVelocities(new Vector2(), new Vector2(1, -1), 1).X.Between(0, 10 / 2));
            Assert.That(ddvsg.PredictWheelVelocities(new Vector2(), new Vector2(1, -1), 1).Y.Between(-10 / 2, 0));
        }

        [Test]
        public void PredictWheelVelocititesConstants()
        {
            Func<DiffDriveVelocitySpaceGenerator, Vector2> pr = ddvsg => ddvsg.PredictWheelVelocities(new Vector2(), new Vector2(1, 1), 1);

            Assert.That(pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 1, 0, 1)).Greater(
                        pr(new DiffDriveVelocitySpaceGenerator(10, 1, 1, 1, 1, 1, 0, 1))));
            Assert.That(pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 1, 0, 1)), Is.EqualTo(
                        pr(new DiffDriveVelocitySpaceGenerator(1, 10, 1, 1, 1, 1, 0, 1))));
            Assert.That(pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 1, 0, 1)).Greater(
                        pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 10, 1, 1, 0, 1))));
            Assert.That(pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 1, 0, 1)), Is.EqualTo(
                        pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 10, 1, 0, 1))));
            Assert.That(pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 10, 0, 1)).Greater(
                        pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 1, 0, 1))));

            pr = ddvsg => ddvsg.PredictWheelVelocities(new Vector2(), new Vector2(-1, 1), 1);

            Assert.That(pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 1, 0, 1)).X, Is.LessThan(
                        pr(new DiffDriveVelocitySpaceGenerator(1, 10, 1, 1, 1, 1, 0, 1)).X));
            Assert.That(pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 1, 0, 1)).Y, Is.GreaterThan(
                        pr(new DiffDriveVelocitySpaceGenerator(1, 10, 1, 1, 1, 1, 0, 1)).Y));
            Assert.That(pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 1, 0, 1)).X, Is.GreaterThan(
                        pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 10, 1, 0, 1)).X));
            Assert.That(pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 1, 1, 0, 1)).Y, Is.LessThan(
                        pr(new DiffDriveVelocitySpaceGenerator(1, 1, 1, 1, 10, 1, 0, 1)).Y));
        }

        [Test]
        public void Generate()
        {
            var ddvsg = new DiffDriveVelocitySpaceGenerator(1, 1, 10, 2, 1, 1, 0, 0.5);
            var vs = ddvsg.Generate(new Velocity(3, 0));

            Assert.That(vs.GetLength(0), Is.EqualTo(2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1));
            Assert.That(vs.GetLength(1), Is.EqualTo(2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1));

            Assert.That(vs[0, 0].WheelAcceleration, Is.EqualTo(new Vector2(-1, -1)));
            Assert.That(vs[0, 1].WheelAcceleration, Is.EqualTo(new Vector2(-1, -0.9f)));
            Assert.That(vs[1, 0].WheelAcceleration, Is.EqualTo(new Vector2(-0.9f, -1)));
            Assert.That(vs[2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER, 2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER].WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));

            Assert.That(vs[0, 0].Velocity, Is.EqualTo(
                    ddvsg.WheelsToRobotKinematics(ddvsg.PredictWheelVelocities(ddvsg.RobotKinematicsToWheels(new Velocity(3, 0)), vs[0, 0].WheelAcceleration, 0.5/2))));
        }
    }
}