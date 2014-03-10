using System;
using Brumba.Utils;
using Brumba.WaiterStupid.McLocalization;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
    [TestFixture]
    public class McLrfLocalizatorTests
    {
        [Test]
        public void GlobalLocalization()
        {
            var mcl = new McLrfLocalizator(
                map: new OccupancyGrid(
                    new[,] { {false, true, false, false, true, false, true, false, false},
                        {false, false, false, false, false, false, false, false, false}}, 1),
                rangefinderProperties: new RangefinderProperties
                {
                    AngularResolution = MathHelper.Pi,
                    AngularRange = MathHelper.Pi,
                    MaxRange = 1.2f,
                    ZeroBeamAngleInRobot = 3 * MathHelper.PiOver2
                }
                );
            //| | | | | | | | | |
            //| |O| | |O| |O| | |
            // 0 1 2 3 4 5 6 7 8
            // right-down, right-none, right-down => 6

            Assert.Fail();
        }

        [Test]
        public void InitPoseUnknown()
        {
            var mcl = new McLrfLocalizator(
                map: new OccupancyGrid(
                    new[,] {{false, true, false, false},
                        {false, false, false, false},
                        {false, false, true, false}}, 1),
                rangefinderProperties: new RangefinderProperties
                {
                    AngularResolution = MathHelper.Pi,
                    AngularRange = MathHelper.Pi,
                    MaxRange = 1.2f,
                    ZeroBeamAngleInRobot = 3 * MathHelper.PiOver2
                }
                );

            mcl.InitPoseUnknown();

            Assert.That(mcl.CalculatePoseExpectation().EqualsRelatively(new Vector3(2, 1.5f, MathHelper.Pi), 0.1));
            Assert.That(mcl.CalculatePoseStdDev().EqualsRelatively(new Vector3(2 / (float)Math.Sqrt(3), (float)Math.Sqrt(3) / 2, MathHelper.Pi / (float)Math.Sqrt(3)), 0.1));
        }
    }
}