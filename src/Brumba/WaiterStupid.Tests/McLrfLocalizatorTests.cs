using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Brumba.WaiterStupid.McLocalization;
using MathNet.Numerics.Statistics;
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
                    new[,] { {true, false, false, true, false, false, true, false, false},
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
            //|_|_|_|_|_|_|_|_|_|
            //|O| | |O| | |O| | |
            // 0 1 2 3 4 5 6 7 8

			mcl.InitPoseUnknown();

//            Console.WriteLine(mcl);
            mcl.Update(new Vector3(0.5f, 0, 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Vector3(2, 0, 0), new[] { 5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Vector3(0.5f, 0, 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Vector3(0.5f, 0, 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Vector3(0.5f, 0, 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Vector3(2, 0, 0), new[] { 5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Vector3(0.5f, 0, 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Vector3(0, 0, 0), new[] { 0.5f, 5 });
            var h = new PoseHistogram(mcl.Map, McLrfLocalizator.THETA_BIN_SIZE);
            h.Build(mcl.Particles);
            Console.WriteLine(h);
            Console.WriteLine("Total particles: {0, 5}; Outside of map: {1, 5}", mcl.Particles.Count(), mcl.Particles.Count(p => !mcl.Map.Covers(p.ExtractVector2())));

            var poseMean = mcl.CalculatePoseMean();
            Console.WriteLine("Pose mean = {0}", poseMean);
			Assert.That(poseMean.ExtractVector2().EqualsRelatively(new Vector2(6.5f, 1.5f), 0.1));
            Assert.That(poseMean.Z.ToMinAbsValueAngle(), Is.EqualTo(0).Within(MathHelper.Pi / 9));

            var firsPoseCandidate = mcl.GetPoseCandidates().First();
            Console.WriteLine("First candidate pose = {0}", firsPoseCandidate);
            Assert.That(firsPoseCandidate.ExtractVector2().EqualsRelatively(new Vector2(6.5f, 1.5f), 0.1));
            Assert.That(firsPoseCandidate.Z.ToMinAbsValueAngle(), Is.EqualTo(0).Within(MathHelper.Pi / 9));
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
                    MaxRange = 5f,
                    ZeroBeamAngleInRobot = 3 * MathHelper.PiOver2
				},
				particlesNumber: 1000
                );

            mcl.InitPoseUnknown();

            Assert.That(mcl.Particles.Count(), Is.EqualTo(1000));
            Assert.That(mcl.Particles.All(p => mcl.Map.Covers(p.ExtractVector2()) && !mcl.Map[p.ExtractVector2()]));

            var poseMean = mcl.CalculatePoseMean();
            Assert.That(poseMean.ExtractVector2().EqualsRelatively(new Vector2(2f, 1.5f), 0.1));

            Assert.That(mcl.CalculatePoseStdDev().EqualsRelatively(new Vector3(2f / (float)Math.Sqrt(3), (float)Math.Sqrt(3) / 2f, MathHelper.Pi / (float)Math.Sqrt(3)), 0.1));
        }

        [Test]
        public void InitPose()
        {
            var mcl = new McLrfLocalizator(
                map: new OccupancyGrid(new[,] 
                    {{false, false, false},
                     {false, false, false},
                     {false, false, false}}, 1),
                rangefinderProperties: new RangefinderProperties
                {
                    AngularResolution = MathHelper.Pi,
                    AngularRange = MathHelper.Pi,
                    MaxRange = 5f,
                    ZeroBeamAngleInRobot = 3 * MathHelper.PiOver2
                },
                particlesNumber: 1000
                );

            mcl.InitPose(poseMean: new Vector3(1.5f, 1.5f, MathHelper.PiOver4), poseStdDev: new Vector3(0.1f));

            Assert.That(mcl.GetPoseCandidates().First().EqualsRelatively(new Vector3(1.5f, 1.5f, MathHelper.PiOver4), 0.1));

            var poseMean = mcl.CalculatePoseMean();
            Assert.That(poseMean.ExtractVector2().EqualsRelatively(new Vector2(1.5f, 1.5f), 0.1));
            Assert.That(poseMean.Z.ToMinAbsValueAngle(), Is.EqualTo(MathHelper.PiOver4).Within(MathHelper.Pi / 9));

            Assert.That(mcl.CalculatePoseStdDev().EqualsRelatively(new Vector3(0.1f), 0.1));
        }
    }
}