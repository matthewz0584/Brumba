using System;
using System.Linq;
using Brumba.Simulation.SimulatedStabilizer.Proxy;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Brumba.VehicleBrains.Behaviours.AirborneStabilizerBehaviour.Tests
{
    [TestFixture]
    public class AirborneStabilizerBehaviourTests
    {
        private AirborneStabilizerBehaviour.Calculator _asbCalc;
        private const int SCAN_PERIOD = 50;

        [SetUp]
        public void SetUp()
        {
            _asbCalc = new AirborneStabilizerBehaviour.Calculator(new AirborneStabilizerBehaviourState
                {
                    LfRangefinderPosition = new Vector3(-1, 0, 1),
                    RfRangefinderPosition = new Vector3(1, 0, 1),
                    LrRangefinderPosition = new Vector3(-1, 0, -1),
                    RrRangefinderPosition = new Vector3(1, 0, -1),
                    ScanInterval = SCAN_PERIOD
                });
        }

        [Test]
        public void CalculateGroundPlaneNormalPrecise()
        {
            var points = new[] {new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 1)};
            var gpNormal = _asbCalc.CalculateGroundPlaneNormal(points);
            
            AssertVectorsAreEqual(gpNormal, Vector3.Normalize(new Vector3(1, 1, 1)));

            points = new[] {new Vector3(-1, 0, 1), new Vector3(1, -2, 1), new Vector3(-1, 0, -1), new Vector3(1, -2, -1)};
            gpNormal = _asbCalc.CalculateGroundPlaneNormal(points);

            AssertVectorsAreEqual(gpNormal, Vector3.Normalize(new Vector3(-1, 1, 0)));
        }

        [Test]
        public void CalculateGroundPlaneNormalLLS()
        {
            var points = new[] { new Vector3(1, 0, 0), new Vector3(-0.5f, (float)Math.Sin(Math.PI / 3), 0), new Vector3(-0.5f, -(float)Math.Sin(Math.PI / 3), 0), new Vector3(0, 0, 1) };
            var gpNormal = _asbCalc.CalculateGroundPlaneNormal(points);

            AssertVectorsAreEqual(gpNormal, Vector3.UnitZ);
        }

        [Test]
        public void GetGroundPoints()
        {
            var gPoints = _asbCalc.GetGroundPoints(0.1f, 0.2f, 0.3f, 0.4f).ToList();

            Assert.That(gPoints[0], Is.EqualTo(new Vector3(-1, -0.1f, 1)));
            Assert.That(gPoints[1], Is.EqualTo(new Vector3(1, -0.2f, 1)));
            Assert.That(gPoints[2], Is.EqualTo(new Vector3(-1, -0.3f, -1)));
            Assert.That(gPoints[3], Is.EqualTo(new Vector3(1, -0.4f, -1)));
        }

        [Test]
        public void CalculateAngle()
        {
            var angle = _asbCalc.CalculateAngle(Vector3.Normalize(new Vector3(1, 1, 1)));
            Assert.That(angle, Is.EqualTo(MathHelper.PiOver4));

            angle = _asbCalc.CalculateAngle(Vector3.Normalize(new Vector3(-1, 1, 1)));
            Assert.That(angle, Is.EqualTo(MathHelper.Pi * 7 / 4));
        }

        [Test]
        public void CalculateShoulder()
        {
            Assert.That(_asbCalc.CalculateShoulder(Vector3.UnitY), Is.EqualTo(0));

            var gpN = Vector3.Normalize(new Vector3(1, 1, 0));

            //Proportional
            _asbCalc.Td = 0;
            _asbCalc.Ti = float.PositiveInfinity;
            Assert.That(_asbCalc.CalculateShoulder(gpN), 
                Is.EqualTo(_asbCalc.Kp * 0.5f).Within(0.000001));

            //Derivative
            _asbCalc.CalculateShoulder(Vector3.UnitY); // reset old error to zero
            _asbCalc.Td = 100;
            _asbCalc.Ti = float.PositiveInfinity;
            Assert.That(_asbCalc.CalculateShoulder(gpN),
                Is.EqualTo(_asbCalc.Kp * (0.5f + _asbCalc.Td * (0.5f / SCAN_PERIOD))).
                Within(0.00001));

            _asbCalc.Td = 0;
            _asbCalc.Ti = 10;
            Assert.That(_asbCalc.CalculateShoulder(gpN),
                Is.EqualTo(_asbCalc.Kp * (0.5f + 1.0f / _asbCalc.Ti * 2 * 0.5f * SCAN_PERIOD)).
                Within(0.00001));
        }

        [Test]
        public void Cycle()
        {
            var stabState = new SimulatedStabilizerState
                {
                    LfWheelToGroundDistance = 0,
                    RfWheelToGroundDistance = 2,
                    LrWheelToGroundDistance = 0,
                    RrWheelToGroundDistance = 2
                };

            var mtRequest = _asbCalc.Cycle(stabState);

            Assert.That(mtRequest.Angle, Is.EqualTo(3 * MathHelper.PiOver2));
            Assert.That(mtRequest.Shoulder, Is.EqualTo(_asbCalc.Kp * (0.5f + _asbCalc.Td * (0.5f / SCAN_PERIOD))));
        }

        private static void AssertVectorsAreEqual(Vector3 lhs, Vector3 rhs)
        {
            Assert.That(lhs.X, Is.EqualTo(rhs.X).Within(0.000001));
            Assert.That(lhs.Y, Is.EqualTo(rhs.Y).Within(0.000001));
            Assert.That(lhs.Z, Is.EqualTo(rhs.Z).Within(0.000001));
        }
    }
}