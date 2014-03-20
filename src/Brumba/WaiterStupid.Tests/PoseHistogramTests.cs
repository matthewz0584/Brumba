using System;
using System.Linq;
using Brumba.Utils;
using Brumba.WaiterStupid.McLocalization;
using MathNet.Numerics;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
    [TestFixture]
    public class PoseHistogramTests
    {
        [Test]
        public void Build()
        {
            var ph = new PoseHistogram(
                map: new OccupancyGrid(new[,]
                {
                    {false, false},
                    {false, false}
                }, 1),
                thetaBinSize: Math.PI);

            ph.Build(poseSamples: new[]
            {
                new Pose(new Vector2(0.5f, 0.5f), Constants.PiOver2), new Pose(new Vector2(0.5f, 0.5f), 5 * Constants.PiOver4),
                new Pose(new Vector2(1.1f, 1.3f), Constants.PiOver2), new Pose(new Vector2(1.9f, 1.7f), 3 * Constants.PiOver4),
                new Pose(new Vector2(0.5f, 1.7f), Constants.PiOver2), new Pose(new Vector2(5f, 5f), Constants.PiOver2)
            });

            Assert.That(ph.Bins.Count(), Is.EqualTo(8));
            Assert.That(ph.Bins.Select(b => b.Samples.Count()).Sum(), Is.EqualTo(5));

            Assert.That(ph[new Pose(new Vector2(0.5f, 0.5f), Constants.PiOver2)].Samples, Is.EquivalentTo(new[] { new Pose(new Vector2(0.5f, 0.5f), Constants.PiOver2) }));

            Assert.That(ph[new Pose(new Vector2(0.5f, 0.5f), 3 * Constants.PiOver2)].Samples, Is.EquivalentTo(new[] { new Pose(new Vector2(0.5f, 0.5f), 5 * Constants.PiOver4) }));

            Assert.That(ph[new Pose(new Vector2(1.1f, 1.3f), Constants.PiOver2)].Samples, Is.EquivalentTo(new[] { new Pose(new Vector2(1.1f, 1.3f), Constants.PiOver2), new Pose(new Vector2(1.9f, 1.7f), 3 * Constants.PiOver4) }));

            Assert.That(ph[new Pose(new Vector2(0.5f, 1.7f), Constants.PiOver2)].Samples, Is.EquivalentTo(new[] { new Pose(new Vector2(0.5f, 1.7f), Constants.PiOver2) }));
        }

        [Test]
        public void BinCoordIndexer()
        {
            var ph = new PoseHistogram(
                map: new OccupancyGrid(new[,]
                {
                    {false, false},
                    {false, false}
                }, 1),
                thetaBinSize: Math.PI);

            ph.Build(poseSamples: new[]
            {
                new Pose(new Vector2(0.5f, 0.5f), Constants.PiOver2), new Pose(new Vector2(0.5f, 0.5f), 5 * Constants.PiOver4),
                new Pose(new Vector2(1.1f, 1.3f), Constants.PiOver2), new Pose(new Vector2(1.9f, 1.7f), 3 * Constants.PiOver4),
                new Pose(new Vector2(0.5f, 1.7f), Constants.PiOver2), new Pose(new Vector2(5f, 5f), Constants.PiOver2)
            });

            Assert.That(ph[0, 0, 0].Samples, Is.EquivalentTo(new[] { new Pose(new Vector2(0.5f, 0.5f), Constants.PiOver2) }));
            Assert.That(ph[0, 0, 1].Samples, Is.EquivalentTo(new[] { new Pose(new Vector2(0.5f, 0.5f), 5 * Constants.PiOver4) }));
            Assert.That(ph[0, 1, 0].Samples, Is.EquivalentTo(new[] { new Pose(new Vector2(0.5f, 1.7f), Constants.PiOver2) }));
            Assert.That(ph[0, 1, 1].Samples, Is.Empty);
            Assert.That(ph[1, 0, 0].Samples, Is.Empty);
            Assert.That(ph[1, 0, 1].Samples, Is.Empty);
            Assert.That(ph[1, 1, 0].Samples, Is.EquivalentTo(new[] { new Pose(new Vector2(1.1f, 1.3f), Constants.PiOver2), new Pose(new Vector2(1.9f, 1.7f), 3 * Constants.PiOver4) }));
            Assert.That(ph[1, 1, 1].Samples, Is.Empty);
        }

        [Test]
        public void Size()
        {
            var ph = new PoseHistogram(map: new OccupancyGrid(new[,] { { false } }, 1),
                thetaBinSize: Math.PI);
            Assert.That(ph.Size, Is.EqualTo(new Vector3(1, 1, 2)));

            ph = new PoseHistogram(map: new OccupancyGrid(new[,] { { false, false } }, 1),
                thetaBinSize: Math.PI * 4 / 5);
            Assert.That(ph.Size, Is.EqualTo(new Vector3(2, 1, 3)));

            ph = new PoseHistogram(map: new OccupancyGrid(new[,] { { false }, { false } }, 1),
                thetaBinSize: Math.PI * 2 / 3);
            Assert.That(ph.Size, Is.EqualTo(new Vector3(1, 2, 3)));
        }

	    [Test]
		public void ToXyMarginal()
	    {
			var ph = new PoseHistogram(
				map: new OccupancyGrid(new[,]
                {
                    {false, false, false},
                    {false, false, false}
                }, 1),
				thetaBinSize: Math.PI);

			ph.Build(poseSamples: new[]
            {
                new Pose(new Vector2(0.5f, 0.5f), Constants.PiOver2), new Pose(new Vector2(0.5f, 0.5f), 5 * Constants.PiOver4),
                new Pose(new Vector2(1.1f, 1.3f), Constants.PiOver2), new Pose(new Vector2(1.9f, 1.7f), 3 * Constants.PiOver4),
                new Pose(new Vector2(0.5f, 1.7f), Constants.PiOver2), new Pose(new Vector2(2.5f, 1.5f), Constants.PiOver2)
            });

		    var xyM = ph.ToXyMarginal();

			Assert.That(xyM[0, 0], Is.EqualTo(2));
			Assert.That(xyM[1, 0], Is.EqualTo(0));
			Assert.That(xyM[2, 0], Is.EqualTo(0));
			Assert.That(xyM[0, 1], Is.EqualTo(1));
			Assert.That(xyM[1, 1], Is.EqualTo(2));
			Assert.That(xyM[2, 1], Is.EqualTo(1));
	    }

        [Test]
        public void PoseBinAddSample()
        {
            var pb = new PoseHistogram.PoseBin();
            pb.AddSample(new Pose(new Vector2(1, 2), Constants.PiOver2));
            pb.AddSample(new Pose(new Vector2(3, 6), Constants.PiOver4));

            Assert.That(pb.Samples, Is.EquivalentTo(new[] { new Pose(new Vector2(1, 2), Constants.PiOver2), new Pose(new Vector2(3, 6), Constants.PiOver4) }));
        }

        [Test]
        public void PoseBinCalculatePoseMean()
        {
            var pb = new PoseHistogram.PoseBin();
            pb.AddSample(new Pose(new Vector2(1, 2), Constants.PiOver2));
            pb.AddSample(new Pose(new Vector2(3, 6), Constants.PiOver4));

            Assert.That(pb.CalculatePoseMean().Position, Is.EqualTo(new Vector2(2, 4)));
            Assert.That(pb.CalculatePoseMean().Bearing, Is.EqualTo(Constants.Pi * 3 / 8));
        }
    }
}