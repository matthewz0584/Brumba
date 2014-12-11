using System;
using System.Linq;
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
            var ddc = new DiffDriveVelocitySpaceGenerator(1.5d, 3d, 0.1d);

            Assert.That(ddc.WheelsToRobotKinematics(new Vector2(2, 1)), Is.EqualTo(new Velocity(1.5d / 2 * (2 + 1), 1.5d / 3d * (1 - 2))));
        }

        [Test]
        public void Generate()
        {
            var ddc = new DiffDriveVelocitySpaceGenerator(1.5d, 3d, 0.1);
            var dd = ddc.Generate(new Velocity(3, 0));

            var accMax = new Velocity(ddc.WheelsToRobotKinematics(new Vector2(1)).Linear, ddc.WheelsToRobotKinematics(new Vector2(-1, 1)).Angular);

            Assert.That(dd.GetLength(0) * dd.GetLength(1), Is.EqualTo((2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1) * (2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Count(), Is.EqualTo((2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1) * (2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1)));
            Assert.That(dd.Cast<VelocityAcceleration>().Min(v => v.Velocity.Linear), Is.EqualTo(3 - accMax.Linear * 0.1));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 - accMax.Linear * 0.1).Velocity.Angular, Is.EqualTo(0));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 - accMax.Linear * 0.1).WheelAcceleration, Is.EqualTo(new Vector2(-1, -1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Max(v => v.Velocity.Linear), Is.EqualTo(3 + accMax.Linear * 0.1));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 + accMax.Linear * 0.1).Velocity.Angular, Is.EqualTo(0));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 + accMax.Linear * 0.1).WheelAcceleration, Is.EqualTo(new Vector2(1, 1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Min(v => v.Velocity.Angular), Is.EqualTo(-accMax.Angular * 0.1));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Angular == -accMax.Angular * 0.1).Velocity.Linear, Is.EqualTo(3));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Angular == -accMax.Angular * 0.1).WheelAcceleration, Is.EqualTo(new Vector2(1, -1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Max(v => v.Velocity.Angular), Is.EqualTo(accMax.Angular * 0.1));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Angular == accMax.Angular * 0.1).Velocity.Linear, Is.EqualTo(3));
            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Angular == accMax.Angular * 0.1).WheelAcceleration, Is.EqualTo(new Vector2(-1, 1)));

            Assert.That(dd.Cast<VelocityAcceleration>().Single(p => p.Velocity.Linear == 3 && p.Velocity.Angular == 0).WheelAcceleration, Is.EqualTo(new Vector2()));

            Assert.That(dd.Cast<VelocityAcceleration>().Count(v => v.Velocity.Linear == 3), Is.EqualTo(2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1));
            Assert.That(dd.Cast<VelocityAcceleration>().Count(v => v.Velocity.Angular == 0), Is.EqualTo(2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1));

            Assert.That(dd.Cast<VelocityAcceleration>().Count(v => Math.Abs(v.Velocity.Linear - (3 + accMax.Linear * 0.1 / DiffDriveVelocitySpaceGenerator.STEPS_NUMBER)) < 0.0001), Is.EqualTo(2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1 - 2));
            Assert.That(dd.Cast<VelocityAcceleration>().Count(v => Math.Abs(v.Velocity.Angular - accMax.Angular * 0.1 / DiffDriveVelocitySpaceGenerator.STEPS_NUMBER) < 0.0001), Is.EqualTo(2 * DiffDriveVelocitySpaceGenerator.STEPS_NUMBER + 1 - 2));
        }
    }
}