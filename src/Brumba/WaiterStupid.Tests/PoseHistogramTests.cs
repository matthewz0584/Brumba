using System;
using System.Linq;
using Brumba.Utils;
using Brumba.WaiterStupid.McLocalization;
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
                new Vector3(0.5f, 0.5f, MathHelper.PiOver2), new Vector3(0.5f, 0.5f, -MathHelper.PiOver2),
                new Vector3(1.1f, 1.3f, MathHelper.PiOver2), new Vector3(1.9f, 1.7f, 3 * MathHelper.PiOver4),
                new Vector3(0.5f, 1.7f, MathHelper.PiOver2), new Vector3(5f, 5f, MathHelper.PiOver2)
            });

            Assert.That(ph.Bins.Count(), Is.EqualTo(8));
            Assert.That(ph.Bins.Select(b => b.Samples.Count()).Sum(), Is.EqualTo(5));
            
            Assert.That(ph[new Vector3(0.5f, 0.5f, MathHelper.PiOver2)].Samples, Is.EquivalentTo(new[] { new Vector3(0.5f, 0.5f, MathHelper.PiOver2) }));

            Assert.That(ph[new Vector3(0.5f, 0.5f, -MathHelper.PiOver2)].Samples, Is.EquivalentTo(new[] { new Vector3(0.5f, 0.5f, -MathHelper.PiOver2) }));

            Assert.That(ph[new Vector3(1.1f, 1.3f, MathHelper.PiOver2)].Samples, Is.EquivalentTo(new[] { new Vector3(1.1f, 1.3f, MathHelper.PiOver2), new Vector3(1.9f, 1.7f, 3 * MathHelper.PiOver4) }));

            Assert.That(ph[new Vector3(0.5f, 1.7f, MathHelper.PiOver2)].Samples, Is.EquivalentTo(new[] { new Vector3(0.5f, 1.7f, MathHelper.PiOver2) }));
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
                new Vector3(0.5f, 0.5f, MathHelper.PiOver2), new Vector3(0.5f, 0.5f, -MathHelper.PiOver2),
                new Vector3(1.1f, 1.3f, MathHelper.PiOver2), new Vector3(1.9f, 1.7f, 3 * MathHelper.PiOver4),
                new Vector3(0.5f, 1.7f, MathHelper.PiOver2), new Vector3(5f, 5f, MathHelper.PiOver2)
            });

            Assert.That(ph[0, 0, 0].Samples, Is.EquivalentTo(new[] { new Vector3(0.5f, 0.5f, MathHelper.PiOver2) }));
            Assert.That(ph[0, 0, 1].Samples, Is.EquivalentTo(new[] { new Vector3(0.5f, 0.5f, -MathHelper.PiOver2) }));
            Assert.That(ph[0, 1, 0].Samples, Is.EquivalentTo(new[] { new Vector3(0.5f, 1.7f, MathHelper.PiOver2) }));
            Assert.That(ph[0, 1, 1].Samples, Is.Empty);
            Assert.That(ph[1, 0, 0].Samples, Is.Empty);
            Assert.That(ph[1, 0, 1].Samples, Is.Empty);
            Assert.That(ph[1, 1, 0].Samples, Is.EquivalentTo(new[] { new Vector3(1.1f, 1.3f, MathHelper.PiOver2), new Vector3(1.9f, 1.7f, 3 * MathHelper.PiOver4) }));
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
        public void PoseBinAddSample()
        {
            var pb = new PoseHistogram.PoseBin();
            pb.AddSample(new Vector3(1, 2, MathHelper.PiOver2));
            pb.AddSample(new Vector3(3, 6, -MathHelper.PiOver4));

            Assert.That(pb.Samples, Is.EquivalentTo(new [] {new Vector3(1, 2, MathHelper.PiOver2), new Vector3(3, 6, -MathHelper.PiOver4) } ));
        }

        [Test]
        public void PoseBinPoseMean()
        {
            var pb = new PoseHistogram.PoseBin();
            pb.AddSample(new Vector3(1, 2, MathHelper.PiOver2));
            pb.AddSample(new Vector3(3, 6, MathHelper.PiOver4));

            Assert.That(pb.PoseMean(), Is.EqualTo(new Vector3(2, 4, MathHelper.Pi * 3 / 8)));
        }
    }
}