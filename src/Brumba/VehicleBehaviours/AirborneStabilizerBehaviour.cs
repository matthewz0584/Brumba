using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using StbPxy = Brumba.Simulation.SimulatedStabilizer.Proxy;

namespace Brumba.VehicleBrains.Behaviours.AirborneStabilizerBehaviour
{
	[Contract(Contract.Identifier)]
    [DisplayName("Airborne Stabilizer Behaviour")]
    [Description("no description provided")]
	class AirborneStabilizerBehaviour : DsspServiceBase
	{
        public class Calculator
        {
            private readonly AirborneStabilizerBehaviourState _state;

            float _errorOld;
            float _errorIntegral;

            public Calculator(AirborneStabilizerBehaviourState state)
            {
                _state = state;

                Ti = float.PositiveInfinity;
                Td = 100;
                Kp = 10;
            }

            public float Ti { get; set; }
            public float Td { get; set; }
            public float Kp { get; set; }

            public StbPxy.MoveTailRequest Cycle(StbPxy.SimulatedStabilizerState stabState)
            {
                var groundPoints = GetGroundPoints(stabState.LfWheelToGroundDistance, stabState.RfWheelToGroundDistance, stabState.LrWheelToGroundDistance, stabState.RrWheelToGroundDistance);

                var groundPlaneNormal = CalculateGroundPlaneNormal(groundPoints);

                var angle = CalculateAngle(groundPlaneNormal);
                
                //!!!Every new angle (out of deadband) should clear error integral (if integral term is used) and do something with derivative (still don't know what)

                //introduce angle deadband
                var shoulder = CalculateShoulder(groundPlaneNormal);
                //introduce shoulder deadband

                return new StbPxy.MoveTailRequest(angle, shoulder);
            }

            public float CalculateShoulder(Vector3 groundPlaneNormal)
            {
                //PID
                var errorNew = (float)Math.Acos(Vector3.Dot(groundPlaneNormal, Vector3.UnitY)) / MathHelper.PiOver2;
                Debug.Assert(errorNew >= 0 && errorNew < MathHelper.PiOver2);

                //Shoulder value is always positive, it's thes of tail weight's radius in polar coordinates
                var shoulder = Kp * (errorNew + 1 / Ti * _errorIntegral + Td * (errorNew - _errorOld) / _state.ScanPeriod);

                _errorOld = errorNew;
                _errorIntegral += errorNew * _state.ScanPeriod; //unclear how to handle integral term given tail rotation, does it still have any sense

                return shoulder;
            }

            public float CalculateAngle(Vector3 groundPlaneNormal)
            {
                //Vehcile is directed along Z axe (front to positive), altitude is along Y, left is at positive X
                //Z is codirected with Y in 2D
                //Tail angle is laid off counterwise from Y (Z in 3D) axe
                var angleCos = Vector2.Dot(Vector2.Normalize(new Vector2(groundPlaneNormal.X, groundPlaneNormal.Z)), Vector2.UnitY);
                return groundPlaneNormal.X > 0 ? (float)Math.Acos(angleCos) : (MathHelper.TwoPi - (float)Math.Acos(angleCos));
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
                var norm = Matrix.Invert(a) * b;
                return Vector3.Normalize(new Vector3(norm.M11, Math.Abs(norm.M21), norm.M31)); //Normal is directed towards positive Y
            }

            public IEnumerable<Vector3> GetGroundPoints(float lfDistance, float rfDistance, float lrDistance, float rrDistance)
            {
                return new[] { _state.LfRangefinderPosition - Vector3.UnitY * lfDistance,
                            _state.RfRangefinderPosition - Vector3.UnitY * rfDistance,
                            _state.LrRangefinderPosition - Vector3.UnitY * lrDistance,
                            _state.RrRangefinderPosition - Vector3.UnitY * rrDistance};
            }

            static Vector4 Vector4From(IList<float> numbers)
            {
                return new Vector4(numbers[0], numbers[1], numbers[2], numbers[3]);
            }
        }

		[ServiceState]
		AirborneStabilizerBehaviourState _state = new AirborneStabilizerBehaviourState();
		
		[ServicePort("/SimulatedStabilizer", AllowMultipleInstances = true)]
		AirborneStabilizerBehaviourOperations _mainPort = new AirborneStabilizerBehaviourOperations();
        
        [Partner("Stabilizer", Contract = StbPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        StbPxy.SimulatedStabilizerOperations _stabilizer = new StbPxy.SimulatedStabilizerOperations();

	    readonly Calculator _c;

        public AirborneStabilizerBehaviour(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
            _c = new Calculator(_state);
		}

		protected override void Start()
		{
            InitState();

		    base.Start();
		    
            SpawnIterator(Execute);
		}

	    [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        void OnReplace(Replace replaceRequest)
        {
            _state = replaceRequest.Body;
            replaceRequest.ResponsePort.Post(DefaultReplaceResponseType.Instance);
        }

        IEnumerator<ITask> Execute()
        {
            while (true)
            {
                StbPxy.SimulatedStabilizerState stabState = null;
                yield return Arbiter.Choice(_stabilizer.Get(), st => stabState = st, LogError);

                var mtRequest = _c.Cycle(stabState);

                _stabilizer.MoveTail(mtRequest);

                yield return To.Exec(TimeoutPort(_state.ScanPeriod));
            }
        }

	    void InitState()
        {
            _state.LfRangefinderPosition = new Vector3(-0.05f, -0.01f, 0.1f);
            _state.RfRangefinderPosition = new Vector3(0.05f, -0.01f, 0.1f);
            _state.LrRangefinderPosition = new Vector3(-0.05f, -0.01f, -0.1f);
            _state.RrRangefinderPosition = new Vector3(0.05f, -0.01f, -0.1f);

	        _state.ScanPeriod = 50;
        }
	}
}