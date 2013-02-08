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
						VehicleWheelsSpacing = 2.5f,
                        TailMass = 0.3f,
                        TailSegment1Length = 2,
                        TailSegment2Length = 2,
                        VehicleMass = 3,
                        VehicleCmHeight = 4
                    });
        }

        [Test]
        public void ZeroVelocityZeroSteeringResponse()
        {            
            Assert.That(_c.Calculate(0, 0).X, Is.EqualTo(0));
			Assert.That(_c.Calculate(0, 0).Y, Is.EqualTo(OnGroundTailBehaviour.Calculator.MinSegment2Angle).Within(0.00001));
        }

        [Test]
        public void NotZeroVelocityZeroSteeringResponse()
        {
            Assert.That(_c.Calculate(0, 1).X, Is.EqualTo(0));
			Assert.That(_c.Calculate(0, 1).Y, Is.EqualTo(OnGroundTailBehaviour.Calculator.MinSegment2Angle).Within(0.00001));
            Assert.That(_c.Calculate(0, -1).X, Is.EqualTo(0));
			Assert.That(_c.Calculate(0, -1).Y, Is.EqualTo(OnGroundTailBehaviour.Calculator.MinSegment2Angle).Within(0.00001));
        }

        [Test]
        public void ZeroVelocityNotZeroSteeringResponse()
        {
			Assert.That(_c.Calculate(MathHelper.PiOver4, 0).X, Is.EqualTo(0));
			Assert.That(_c.Calculate(MathHelper.PiOver4, 0).Y, Is.EqualTo(OnGroundTailBehaviour.Calculator.MinSegment2Angle).Within(0.00001));
			Assert.That(_c.Calculate(MathHelper.PiOver4, 0).X, Is.EqualTo(0));
			Assert.That(_c.Calculate(MathHelper.PiOver4, 0).Y, Is.EqualTo(OnGroundTailBehaviour.Calculator.MinSegment2Angle).Within(0.00001));
        }

        [Test]
        public void Segment1Response()
        {
			Assert.That(_c.Calculate(MathHelper.PiOver4, 1).X, Is.EqualTo(OnGroundTailBehaviour.Calculator.LimitSegment1Angle));
			Assert.That(_c.Calculate(-MathHelper.PiOver4, 1).X, Is.EqualTo(-OnGroundTailBehaviour.Calculator.LimitSegment1Angle));

			Assert.That(_c.Calculate(MathHelper.PiOver4, -1).X, Is.EqualTo(OnGroundTailBehaviour.Calculator.LimitSegment1Angle));
			Assert.That(_c.Calculate(-MathHelper.PiOver4, -1).X, Is.EqualTo(-OnGroundTailBehaviour.Calculator.LimitSegment1Angle));
        }

        [Test]
        public void Segment2Response()
        {
            //Steeper turn, more distance to the tail mass, i.e less segment2 angle (0 is the longest tail) 
            Assert.That(Math.Abs(_c.Calculate(MathHelper.Pi / 8, 3).Y), Is.GreaterThan(Math.Abs(_c.Calculate(MathHelper.Pi / 6, 3).Y)));
            Assert.That(Math.Abs(_c.Calculate(-MathHelper.Pi / 8, 3).Y), Is.GreaterThan(Math.Abs(_c.Calculate(-MathHelper.Pi / 6, 3).Y)));

            //Higher velocity, more distance to the tail mass
            Assert.That(Math.Abs(_c.Calculate(MathHelper.Pi / 8, 3).Y), Is.GreaterThan(Math.Abs(_c.Calculate(MathHelper.Pi / 8, 4).Y)));
			Assert.That(Math.Abs(_c.Calculate(-MathHelper.Pi / 8, 3).Y), Is.GreaterThan(Math.Abs(_c.Calculate(-MathHelper.Pi / 8, 4).Y)));
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