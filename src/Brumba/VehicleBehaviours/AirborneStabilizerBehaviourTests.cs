using System;
using System.Collections.Generic;
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
        private AirborneStabilizerBehaviourState _asbState;
        private const int SCAN_INTERVAL = 50;

        [SetUp]
        public void SetUp()
        {
            _asbCalc = new AirborneStabilizerBehaviour.Calculator(() => _asbState);
            _asbState = new AirborneStabilizerBehaviourState
                {
                    GroundRangefinderPositions =
                        new List<Vector3>
                            {
                                new Vector3(-1, 0, 1),
                                new Vector3(1, 0, 1),
                                new Vector3(1, 0, -1),
                                new Vector3(-1, 0, -1)
                            },
                    ScanInterval = SCAN_INTERVAL,
                    Kp = 10, Td = 1,
                    TailAngleDeadband = MathHelper.Pi / 180, TailShoulderDeadband = 0.01f
                };
        }

        [Test]
        public void CalculateGroundPlaneNormalPrecise()
        {
            var points = new[] {new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 1)};
            var gpNormal = _asbCalc.CalculateGroundPlaneNormal(points);
            
            AssertVectorsAreEqual(gpNormal, Vector3.Normalize(new Vector3(1, 1, 1)));

            points = new[] { new Vector3(-1, 0, 1), new Vector3(1, -2, 1), new Vector3(1, -2, -1), new Vector3(-1, 0, -1) };
            gpNormal = _asbCalc.CalculateGroundPlaneNormal(points);

            AssertVectorsAreEqual(gpNormal, Vector3.Normalize(new Vector3(1, 1, 0)));
        }

        [Test]
        public void CalculateGroundPlaneNormalLls()
        {
            var points = new[] { new Vector3(1, 0, 0), new Vector3(-0.5f, (float)Math.Sin(Math.PI / 3), 0), new Vector3(-0.5f, -(float)Math.Sin(Math.PI / 3), 0), new Vector3(0, 0, 1) };
            var gpNormal = _asbCalc.CalculateGroundPlaneNormal(points);

            AssertVectorsAreEqual(gpNormal, -Vector3.UnitZ);
        }

        [Test]
        public void GetGroundPoints()
        {
            var gPoints = _asbCalc.GetGroundPoints(new [] {0.1f, 0.2f, 0.3f, 0.4f}).ToList();

            Assert.That(gPoints[0], Is.EqualTo(new Vector3(-1, -0.1f, 1)));
            Assert.That(gPoints[1], Is.EqualTo(new Vector3(1, -0.2f, 1)));
            Assert.That(gPoints[2], Is.EqualTo(new Vector3(1, -0.3f, -1)));
            Assert.That(gPoints[3], Is.EqualTo(new Vector3(-1, -0.4f, -1)));
        }

        [Test]
        public void CalculateAngle()
        {
            var angle = _asbCalc.CalculateAngle(Vector3.Normalize(new Vector3(-1, 1, 1)));
            Assert.That(angle, Is.EqualTo(MathHelper.PiOver4));

            angle = _asbCalc.CalculateAngle(Vector3.Normalize(new Vector3(1, 1, 1)));
            Assert.That(angle, Is.EqualTo(MathHelper.Pi * 7 / 4));

			angle = _asbCalc.CalculateAngle(Vector3.Normalize(new Vector3(0, 1, 0)));
			Assert.That(angle, Is.EqualTo(0));
        }

        [Test]
        public void CalculateShoulder()
        {
            Assert.That(_asbCalc.CalculateShoulder(Vector3.UnitY), Is.EqualTo(0));

            var gpN = Vector3.Normalize(new Vector3(1, 1, 0));

            //Proportional
            _asbState.Td = 0;
            _asbCalc.Ti = float.PositiveInfinity;
            Assert.That(_asbCalc.CalculateShoulder(gpN), 
                Is.EqualTo(_asbState.Kp * 0.5f).Within(0.000001));

            //Derivative
            _asbCalc.CalculateShoulder(Vector3.UnitY); // reset old error to zero
            _asbState.Td = 100;
            _asbCalc.Ti = float.PositiveInfinity;
            Assert.That(_asbCalc.CalculateShoulder(gpN),
                Is.EqualTo(_asbState.Kp * (0.5f + _asbState.Td * (0.5f / SCAN_INTERVAL))).
                Within(0.00001));

            _asbState.Td = 0;
            _asbCalc.Ti = 10;
            Assert.That(_asbCalc.CalculateShoulder(gpN),
                Is.EqualTo(_asbState.Kp * (0.5f + 1.0f / _asbCalc.Ti * 2 * 0.5f * SCAN_INTERVAL)).
                Within(0.00001));
        }

        [Test]
        public void Cycle()
        {
            var angShr = _asbCalc.Cycle(new [] { 0f, 2, 2, 0 });

            Assert.That(angShr.X, Is.EqualTo(3 * MathHelper.PiOver2));
            Assert.That(angShr.Y, Is.EqualTo(_asbState.Kp * (0.5f + _asbState.Td * (0.5f / SCAN_INTERVAL))));
        }

        [Test]
        public void CalculateGroundPlaneNormal2()
        {
            var points = new[] {new Vector3(-1, -6, 2), new Vector3(1, -6, 2), new Vector3(1, -8, -2), new Vector3(-1, -8, -2) };
            var gpNormal = _asbCalc.CalculateGroundPlaneNormal(points);

            Assert.That(gpNormal.X, Is.EqualTo(0).Within(0.00001));
            Assert.That(gpNormal.Y, Is.GreaterThan(0));
            Assert.That(gpNormal.Z, Is.LessThan(0));

//+		[0]	{X:-0,06 Y:-0,3977476 Z:0,11}	Microsoft.Xna.Framework.Vector3
//+		[1]	{X:0,06 Y:-0,3977476 Z:0,11}	Microsoft.Xna.Framework.Vector3
//+		[2]	{X:0,06 Y:-0,5107096 Z:-0,11}	Microsoft.Xna.Framework.Vector3
//+		[3]	{X:-0,06 Y:-0,5107096 Z:-0,11}	Microsoft.Xna.Framework.Vector3
            
        }

        [Test]
        public void Deadbands()
        {
            _asbCalc.Cycle(new [] { 0f, 2, 2, 0 });

            //Derivative changes, shoulder changes too, angle changes are inside deadband
            var angShr = _asbCalc.Cycle(new [] { 0f, 2.05f, 2, 0 });

            Assert.True(float.IsNaN(angShr.X));
            Assert.False(float.IsNaN(angShr.Y));

            angShr = _asbCalc.Cycle(new[] { 0f, 2.06f, 2f, 0 });  //Derivative goes to zero

            Assert.True(float.IsNaN(angShr.X));
            Assert.True(float.IsNaN(angShr.Y));
        }

        private static void AssertVectorsAreEqual(Vector3 lhs, Vector3 rhs)
        {
            Assert.That(lhs.X, Is.EqualTo(rhs.X).Within(0.000001));
            Assert.That(lhs.Y, Is.EqualTo(rhs.Y).Within(0.000001));
            Assert.That(lhs.Z, Is.EqualTo(rhs.Z).Within(0.000001));
        }
    }
}