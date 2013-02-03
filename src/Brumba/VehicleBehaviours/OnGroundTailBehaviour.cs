using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using Brumba.Simulation.SimulatedTail.Proxy;

namespace Brumba.VehicleBrains.Behaviours.OnGroundTailBehaviour
{
	[Contract(Contract.Identifier)]
//    [DisplayName("On Ground Tail Behaviour")]
    [Description("no description provided")]
	class OnGroundTailBehaviour : DsspServiceBase
	{
        public class Calculator
        {
            private OnGroundTailBehaviourState _behState;

            public Calculator(OnGroundTailBehaviourState behState)
            {
                BehState = behState;
            }

            public OnGroundTailBehaviourState BehState
            {
                get { return _behState; }
                set { _behState = value; }
            }

            public Vector2 Calculate(float steerAngle, float velocity)
            {
                var seg1Angle = (velocity == 0 ? 0 : 1) * Math.Sign(steerAngle) * MathHelper.PiOver2;
                var seg2AngleCos = BehState.VehicleMass/BehState.TailMass*CentripetalA(steerAngle, velocity)/9.8f*
                              BehState.VehicleCmHeight/BehState.TailSegment2Length -
                              BehState.TailSegment1Length/BehState.TailSegment2Length;
                var seg2Angle = (float)Math.Acos(MathHelper.Clamp(seg2AngleCos, 0, 1));
                seg2Angle = MathHelper.Clamp(seg2Angle, 0, MathHelper.PiOver2);
                return new Vector2(seg1Angle, -seg2Angle);
            }

            public float CentripetalA(float steerAngle, float velocity)
            {
                return Math.Abs(velocity * velocity / (_behState.VehicleWheelBase / 2 / (float)Math.Tan(steerAngle / 2)));
            }
        }

		[ServiceState]
		OnGroundTailBehaviourState _state = new OnGroundTailBehaviourState();

		[ServicePort("/OnGroundTailBehaviour", AllowMultipleInstances = true)]
		OnGroundTailBehaviourOperations _mainPort = new OnGroundTailBehaviourOperations();

        [Partner("Tail", Contract = Simulation.SimulatedTail.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SimulatedTailOperations _tail = new SimulatedTailOperations();

        [Partner("Vehicle", Contract = Simulation.SimulatedAckermanFourWheels.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SimulatedAckermanFourWheelsOperations _vehicle = new SimulatedAckermanFourWheelsOperations();

	    private Calculator _calculator;

	    public OnGroundTailBehaviour(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
            InitState();
            _calculator = new Calculator(_state);
		}

		protected override void Start()
		{
		    base.Start();

		    SpawnIterator(Execute);
		}

	    [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnReplace(Replace replaceRequest)
        {
            _state = replaceRequest.Body;
	        _calculator.BehState = _state;
            replaceRequest.ResponsePort.Post(DefaultReplaceResponseType.Instance);
        }

        IEnumerator<ITask> Execute()
        {
            while (true)
            {
                SimulatedAckermanFourWheelsState vehState = null;
                yield return Arbiter.Receive<SimulatedAckermanFourWheelsState>(false, _vehicle.Get(), st => vehState = st);

                var angles =
                    _calculator.Calculate(
                        Math.Abs(vehState.ActualSteerAngle) > MathHelper.Pi/50 ? vehState.ActualSteerAngle : 0,
                        vehState.Velocity);

                _tail.ChangeSegment1Angle(angles.X);
                _tail.ChangeSegment2Angle(angles.Y);

                yield return To.Exec(TimeoutPort(100));
            }
        }

	    private void InitState()
	    {
            _state = new OnGroundTailBehaviourState
	            {
                    TailMass = 0.04f,
                    TailSegment1Length = 0.2f,
                    TailSegment2Length = 0.2f,
                    VehicleMass = 2,
                    VehicleCmHeight = 0.1f,
                    VehicleWheelBase = 0.25f
	            };
	    }
	}
}