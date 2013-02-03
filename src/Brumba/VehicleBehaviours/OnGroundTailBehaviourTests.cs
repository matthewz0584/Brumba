using System;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.VehicleBrains.Behaviours.OnGroundTailBehaviour.Tests
{
    [TestFixture]
    public class OnGroundTailBehaviourTests
    {
        private OnGroundTailBehaviour.Calculator _c;

        [SetUp]
        public void SetUp()
        {
            _c = new OnGroundTailBehaviour.Calculator(new OnGroundTailBehaviourState()
                    {
                        VehicleWheelBase = 5,
                        TailMass = 0.3f,
                        TailSegment1Length = 2,
                        TailSegment2Length = 2,
                        VehicleMass = 3,
                        VehicleCmHeight = 4
                    });
        }

        [Test]
        public void ZeroVelocityZeroSteeringResponce()
        {            
            Assert.That(_c.Calculate(0, 0), Is.EqualTo(new Vector2(0, -MathHelper.PiOver2)));
        }

        [Test]
        public void NotZeroVelocityZeroSteeringResponce()
        {
            Assert.That(_c.Calculate(0, 1), Is.EqualTo(new Vector2(0, -MathHelper.PiOver2)));
            Assert.That(_c.Calculate(0, -1), Is.EqualTo(new Vector2(0, -MathHelper.PiOver2)));
        }

        [Test]
        public void ZeroVelocityNotZeroSteeringResponce()
        {
            Assert.That(_c.Calculate(MathHelper.PiOver4, 0), Is.EqualTo(new Vector2(0, -MathHelper.PiOver2)));
            Assert.That(_c.Calculate(MathHelper.PiOver4, 0), Is.EqualTo(new Vector2(0, -MathHelper.PiOver2)));
        }

        [Test]
        public void Segment1Responce()
        {
            Assert.That(_c.Calculate(MathHelper.PiOver4, 1).X, Is.EqualTo(MathHelper.PiOver2));
            Assert.That(_c.Calculate(-MathHelper.PiOver4, 1).X, Is.EqualTo(-MathHelper.PiOver2));

            Assert.That(_c.Calculate(MathHelper.PiOver4, -1).X, Is.EqualTo(MathHelper.PiOver2));
            Assert.That(_c.Calculate(-MathHelper.PiOver4, -1).X, Is.EqualTo(-MathHelper.PiOver2));
        }

        [Test]
        public void Segment2Responce()
        {
            //More steeper turn, more distance to tail mass, i.e less segment2 angle
            Assert.That(_c.Calculate(MathHelper.Pi / 8, 3).Y, Is.LessThan(_c.Calculate(MathHelper.Pi / 6, 3).Y));
            Assert.That(_c.Calculate(-MathHelper.Pi / 8, 3).Y, Is.LessThan(_c.Calculate(-MathHelper.Pi / 6, 3).Y));

            //More velocity more distance to the tail mass
            Assert.That(_c.Calculate(MathHelper.Pi / 8, 2).Y, Is.LessThan(_c.Calculate(MathHelper.Pi / 8, 3).Y));
            Assert.That(_c.Calculate(-MathHelper.Pi / 8, 2).Y, Is.LessThan(_c.Calculate(-MathHelper.Pi / 8, 3).Y));
        }

        [Test]
        public void Segment2Saturation()
        {
            Assert.That(_c.Calculate(MathHelper.Pi / 8, 100).Y, Is.EqualTo(0));
            Assert.That(_c.Calculate(0.3f * MathHelper.Pi, 3).Y, Is.EqualTo(0));
        }

        [Test]
        public void CentripetalAcceleration()
        {
            Assert.That(_c.CentripetalA(MathHelper.PiOver2, 1), Is.EqualTo(2 / _c.BehState.VehicleWheelBase));
            Assert.That(_c.CentripetalA(0, 1), Is.EqualTo(0));

            Assert.That(_c.CentripetalA(MathHelper.PiOver4, 3),
                        Is.EqualTo((float) (3*3/(_c.BehState.VehicleWheelBase/2/Math.Tan(MathHelper.PiOver4/2)))));
        }
    }
}