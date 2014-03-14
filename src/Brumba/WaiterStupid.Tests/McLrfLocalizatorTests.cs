using System;
using System.Linq;
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
                    MaxRange = 5,
                    ZeroBeamAngleInRobot = 3 * MathHelper.PiOver2
                },
				particlesNumber: 1000
                );
            //| | | | | | | | | |
            //| |O| | |O| |O| | |
            // 0 1 2 3 4 5 6 7 8
            // right-down, right-none, right-down => 6

			mcl.InitPoseUnknown();

	        mcl.Update(new Vector3(1, 0, 0), new[] { 0.5f, 5 });
			mcl.Update(new Vector3(1, 0, 0), new[] { 5f, 5 });
			mcl.Update(new Vector3(1, 0, 0), new[] { 0.5f, 5 });
			//mcl.Update(new Vector3(0, 0, 0), new[] { 0.5f, 1 });

			Console.WriteLine(mcl.CalculatePoseExpectation());
			Assert.That(mcl.CalculatePoseExpectation().EqualsRelatively(new Vector3(6.5f, 1.5f, 0), 0.1));
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
				},
				particlesNumber: 100000
                );

            mcl.InitPoseUnknown();

			Assert.That(mcl.Particles.All(p => mcl.Map.Covers(p.ExtractVector2()) && !mcl.Map[p.ExtractVector2()]));
			Assert.That(mcl.CalculatePoseExpectation().EqualsRelatively(new Vector3(2, 1.5f, MathHelper.Pi), 0.1));
            Assert.That(mcl.CalculatePoseStdDev().EqualsRelatively(new Vector3(2f / (float)Math.Sqrt(3), (float)Math.Sqrt(3) / 2f, MathHelper.Pi / (float)Math.Sqrt(3)), 0.1));
        }
    }
}