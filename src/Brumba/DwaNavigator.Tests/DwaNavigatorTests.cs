using System.Collections.Generic;
using Brumba.WaiterStupid;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.DwaNavigator.Tests
{
    [TestFixture]
    public class DwaNavigatorTests
    {
        private DwaNavigator _dwan;

        [SetUp]
        public void Setup()
        {
            _dwan = new DwaNavigator(
                wheelAngularAccelerationMax: 4,
                wheelAngularVelocityMax: 13, // 1m/s
                wheelRadius: 0.076,
                wheelBase: 0.3,
                robotRadius: 0.3,
                rangefinderMaxRange: 10,
                dt: 0.25);
        }

        [Test]
        public void AccelerationMax()
        {
            Assert.That(_dwan.AccelerationMax, Is.EqualTo(new Velocity(0.076d / 2 * (4 + 4), 0.076d / 0.3d * (4 + 4))));
        }

        [Test]
        public void VelocityMax()
        {
            Assert.That(_dwan.VelocityMax, Is.EqualTo(new Velocity(0.076d / 2 * (13 + 13), 0.076d / 0.3d * (13 + 13))));
        }

        //[Test]
        public void ClearStraightPath()
        {
            Assert.That(_dwan.Cycle(new Pose(new Vector2(), 0),
                                    new Pose(new Vector2(), 0),
                                    new Vector2(10, 0),
                                    new Vector2[0]),
                Is.EqualTo(new Vector2(1, 1)));
        }

        //[Test]
        public void BasicExampleFromDwaArticle()
        {
            Assert.That(_dwan.Cycle(new Pose(new Vector2(), 0),
                                    new Pose(new Vector2(), 0),
                                    new Vector2(1.3f, -2.1f),
                                    new []
                                    {
                                        new Vector2(1.5f, -0.9f), new Vector2(0.3f, -0.9f), new Vector2(0.1f, -0.9f), new Vector2(-0.1f, -0.9f), new Vector2(-0.3f, -0.9f)
                                    }),
                Is.EqualTo(new Vector2(1, 1)));
        }
    }
}