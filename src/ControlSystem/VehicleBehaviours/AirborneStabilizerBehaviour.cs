using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using TailPxy = Brumba.Simulation.SimulatedTail.Proxy;

namespace Brumba.VehicleBrains.Behaviours.AirborneStabilizerBehaviour
{
	[Contract(Contract.Identifier)]
    [DisplayName("Airborne Stabilizer Behaviour")]
    [Description("no description provided")]
	class AirborneStabilizerBehaviour : DsspServiceBase
	{
        public class Calculator
        {
            private AirborneStabilizerBehaviourState _behState;

            float _errorPrev, _errorIntegral;
            float _shoulderPrev, _anglePrev;

            public Calculator(AirborneStabilizerBehaviourState behState)
            {
                _behState = behState;

                Ti = float.PositiveInfinity;
            }

            public void UpdateState(AirborneStabilizerBehaviourState behState)
            {
                _behState = behState;
            }

            public float Ti { get; set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="wheelToGroundDistances"></param>
            /// <returns>Vector2.X - tail angle, Vector2.Y - tail shoulder</returns>
            public Vector2 Cycle(IEnumerable<float> wheelToGroundDistances)
            {
                var groundPoints = GetGroundPoints(wheelToGroundDistances);

                var groundPlaneNormal = CalculateGroundPlaneNormal(groundPoints);

                var angle = CalculateAngle(groundPlaneNormal);                
                //!!!Every new angle (out of deadband) should clear error integral (if integral term is used) and do something with derivative (still don't know what)
                var shoulder = CalculateShoulder(groundPlaneNormal);

                return new Vector2(
                        Math.Abs(angle - _anglePrev) > _behState.TailAngleDeadband
                            ? _anglePrev = angle : float.NaN,
                        Math.Abs(shoulder - _shoulderPrev) > _behState.TailShoulderDeadband
                            ? _shoulderPrev = shoulder : float.NaN);
            }

            public float CalculateShoulder(Vector3 groundPlaneNormal)
            {
                //PID
                var errorNew = (float)Math.Acos(Vector3.Dot(groundPlaneNormal, Vector3.UnitY)) / MathHelper.PiOver2;
                Debug.Assert(errorNew >= 0 && errorNew < MathHelper.PiOver2);

                //Shoulder value is always positive, it's thes of tail weight's radius in polar coordinates
                var shoulder = _behState.Kp * (errorNew + 1 / Ti * _errorIntegral + _behState.Td * (errorNew - _errorPrev) / _behState.ScanInterval);

                _errorPrev = errorNew;
                _errorIntegral += errorNew * _behState.ScanInterval; //unclear how to handle integral term given tail rotation, does it still have any sense

                return shoulder;
            }

            public float CalculateAngle(Vector3 groundPlaneNormal)
            {
                //Vehcile is directed along Z axe (front to positive), altitude is along Y, left is at positive X
                //Z is codirected with Y in 2D
                //Tail angle is laid off counterwise from Y (Z in 3D) axe
				if (new Vector2(groundPlaneNormal.X, groundPlaneNormal.Z).Length() == 0)
					return 0;
                var angleCos = Vector2.Dot(Vector2.Normalize(new Vector2(groundPlaneNormal.X, groundPlaneNormal.Z)), Vector2.UnitY);
                return groundPlaneNormal.X > 0 ? (MathHelper.TwoPi - (float)Math.Acos(angleCos)) : (float)Math.Acos(angleCos);
            }

            public Vector3 CalculateGroundPlaneNormal(IEnumerable<Vector3> groundPoints)
            {
                var xs = Vector4From(groundPoints.Select(v => v.X).ToList());
                var ys = Vector4From(groundPoints.Select(v => v.Y).ToList());
                var zs = Vector4From(groundPoints.Select(v => v.Z).ToList());

                var a = new Matrix(Vector4.Dot(xs, xs), Vector4.Dot(xs, ys), Vector4.Dot(xs, zs), 0,
                                   Vector4.Dot(ys, xs), Vector4.Dot(ys, ys), Vector4.Dot(ys, zs), 0,
                                   Vector4.Dot(zs, xs), Vector4.Dot(zs, ys), Vector4.Dot(zs, zs), 0,
                                   0, 0, 0, 1);
                var ones = new Vector4(1, 1, 1, 1);
                var b = new Matrix(Vector4.Dot(xs, ones), 0, 0, 0,
                                   Vector4.Dot(ys, ones), 0, 0, 0,
                                   Vector4.Dot(zs, ones), 0, 0, 0,
                                   1, 0, 0, 0);
                var normAsM = Matrix.Invert(a) * b;
                var norm = Vector3.Normalize(new Vector3(normAsM.M11, normAsM.M21, normAsM.M31));
                return norm.Y > 0 ? norm : -norm; //Normal is directed towards positive Y
            }

            public IEnumerable<Vector3> GetGroundPoints(IEnumerable<float> wheelToGroundDistances)
            {
                return _behState.GroundRangefinderPositions.Zip(wheelToGroundDistances, (p, d) => p - Vector3.UnitY * d);
            }

            static Vector4 Vector4From(IList<float> numbers)
            {
                return new Vector4(numbers[0], numbers[1], numbers[2], numbers[3]);
            }
        }

		[ServiceState]
		AirborneStabilizerBehaviourState _state = new AirborneStabilizerBehaviourState();

		[ServicePort("/AirborneStabilizerBehaviour", AllowMultipleInstances = true)]
		AirborneStabilizerBehaviourOperations _mainPort = new AirborneStabilizerBehaviourOperations();

        [Partner("Stabilizer", Contract = TailPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        TailPxy.SimulatedTailOperations _stabilizer = new TailPxy.SimulatedTailOperations();

	    readonly Calculator _c;

        public AirborneStabilizerBehaviour(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
            InitState();
            _c = new Calculator(_state);
		}

		protected override void Start()
		{
		    base.Start();

            _stabilizer.ChangeSegment2Angle(MathHelper.Pi / 2);
		    _stabilizer.ChangeSegment1Angle(MathHelper.Pi / 2);
		    //SpawnIterator(Execute);
		}

	    [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnReplace(Replace replaceRequest)
        {
            _state = replaceRequest.Body;
	        _c.UpdateState(_state);
            replaceRequest.ResponsePort.Post(DefaultReplaceResponseType.Instance);
        }

        IEnumerator<ITask> Execute()
        {
            while (true)
            {
                TailPxy.SimulatedTailState stabState = null;
                yield return Arbiter.Choice(_stabilizer.Get(), st => stabState = st, LogError);
				if (!stabState.IsConnected)
					continue;

                var angleShoulder = _c.Cycle(stabState.WheelToGroundDistances);

                //if (!float.IsNaN(angleShoulder.X))
                //    _stabilizer.ChangeTailAngle(angleShoulder.X);
                //if (!float.IsNaN(angleShoulder.Y))
                //    _stabilizer.ChangeTailShoulder(angleShoulder.Y);

                yield return TimeoutPort((int)(_state.ScanInterval * 1000)).Receive();
            }
        }

	    private void InitState()
	    {
	        _state = new AirborneStabilizerBehaviourState
	            {
	                GroundRangefinderPositions = new List<Vector3>
	                    {
	                        new Vector3(-0.06f, 0, 0.11f),
	                        new Vector3(0.06f, 0, 0.11f),
	                        new Vector3(0.06f, 0, -0.11f),
	                        new Vector3(-0.06f, 0, -0.11f)
	                    },
	                ScanInterval = 0.025f,
	                Kp = 0.1f,
	                Td = 4f,
	                //TailAngleDeadband = MathHelper.Pi/180,
	                //TailShoulderDeadband = 0.005f
	            };
	    }
	}
}