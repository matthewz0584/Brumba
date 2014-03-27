using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Brumba.WaiterStupid.McLocalization;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
    [TestFixture]
    public class McLrfLocalizerTests
    {
        [Test]
        public void GlobalLocalization()
        {
            var mcl = new McLrfLocalizer(
                map: new OccupancyGrid(
                    new[,] { {true, false, false, true, false, false, true, false, false},
                        {false, false, false, false, false, false, false, false, false}}, 1),
                rangefinderProperties: new RangefinderProperties
                {
                    AngularResolution = Constants.Pi,
                    AngularRange = Constants.Pi,
                    MaxRange = 5,
                    ZeroBeamAngleInRobot = 3 * Constants.PiOver2
                },
				particlesNumber: 1000
                );
            //|_|_|_|_|_|_|_|_|_|
            //|O| | |O| | |O| | |
            // 0 1 2 3 4 5 6 7 8

			mcl.InitPoseUnknown();

//            Console.WriteLine(mcl);
            mcl.Update(new Pose(new Vector2(0.5f, 0), 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Pose(new Vector2(2, 0), 0), new[] { 5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Pose(new Vector2(0.5f, 0), 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Pose(new Vector2(0.5f, 0), 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Pose(new Vector2(0.5f, 0), 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Pose(new Vector2(2, 0), 0), new[] { 5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Pose(new Vector2(0.5f, 0), 0), new[] { 0.5f, 5 });
//            Console.WriteLine(mcl);
            mcl.Update(new Pose(new Vector2(0, 0), 0), new[] { 0.5f, 5 });
            var h = new PoseHistogram(mcl.Map, McLrfLocalizer.THETA_BIN_SIZE);
            h.Build(mcl.Particles);
            Console.WriteLine(h);
            Console.WriteLine("Total particles: {0, 5}; Outside of map: {1, 5}", mcl.Particles.Count(), mcl.Particles.Count(p => !mcl.Map.Covers(p.Position)));

            var poseMean = mcl.CalculatePoseMean();
            Console.WriteLine("Pose mean = {0}", poseMean);
			Assert.That(poseMean.Position.EqualsRelatively(new Vector2(6.5f, 1.5f), 0.1));
            Assert.That(poseMean.Bearing.ToMinAbsValueAngle(), Is.EqualTo(0).Within(Constants.Pi / 9));

            var firsPoseCandidate = mcl.GetPoseCandidates().First();
            Console.WriteLine("First candidate pose = {0}", firsPoseCandidate);
            Assert.That(firsPoseCandidate.Position.EqualsRelatively(new Vector2(6.5f, 1.5f), 0.1));
            Assert.That(firsPoseCandidate.Bearing.ToMinAbsValueAngle(), Is.EqualTo(0).Within(Constants.Pi / 9));
        }

        [Test]
        public void InitPoseUnknown()
        {
            var mcl = new McLrfLocalizer(
                map: new OccupancyGrid(
                    new[,] {{false, true, false, false},
                        {false, false, false, false},
                        {false, false, true, false}}, 1),
                rangefinderProperties: new RangefinderProperties
                {
                    AngularResolution = Constants.Pi,
                    AngularRange = Constants.Pi,
                    MaxRange = 5f,
                    ZeroBeamAngleInRobot = 3 * Constants.PiOver2
				},
				particlesNumber: 1000
                );

            mcl.InitPoseUnknown();

            Assert.That(mcl.Particles.Count(), Is.EqualTo(1000));
            Assert.That(mcl.Particles.All(p => mcl.Map.Covers(p.Position) && !mcl.Map[p.Position]));

            var poseMean = mcl.CalculatePoseMean();
            Assert.That(poseMean.Position.EqualsRelatively(new Vector2(2f, 1.5f), 0.1));

            var poseStdDev = mcl.CalculatePoseStdDev();
            Assert.That(poseStdDev.Position.EqualsRelatively(new Vector2(2f / (float)Math.Sqrt(3), (float)Math.Sqrt(3) / 2f), 0.1));
            Assert.That(poseStdDev.Bearing, Is.EqualTo(MathHelper.Pi / (float)Math.Sqrt(3)).Within(0.1));
        }

        [Test]
        public void InitPose()
        {
            var mcl = new McLrfLocalizer(
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

            mcl.InitPose(poseMean: new Pose(new Vector2(1.5f, 1.5f), Constants.PiOver4), poseStdDev: new Pose(new Vector2(0.1f), 0.1));

            var firstCandidate = mcl.GetPoseCandidates().First();
            Assert.That(firstCandidate.Position.EqualsRelatively(new Vector2(1.5f, 1.5f), 0.1));
            Assert.That(firstCandidate.Bearing, Is.EqualTo(Constants.PiOver4).Within(0.1));

            var poseMean = mcl.CalculatePoseMean();
            Assert.That(poseMean.Position.EqualsRelatively(new Vector2(1.5f, 1.5f), 0.1));
            Assert.That(poseMean.Bearing.ToMinAbsValueAngle(), Is.EqualTo(Constants.PiOver4).Within(Constants.Pi / 9));

            Assert.That(mcl.CalculatePoseStdDev().Position.EqualsRelatively(new Vector2(0.1f), 0.1));
            Assert.That(mcl.CalculatePoseStdDev().Bearing, Is.EqualTo(0.1).Within(0.01));
        }
    }
}