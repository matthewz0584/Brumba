using System.Collections.Generic;
using System.ComponentModel;
using Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Xna.Framework;
using Brumba.Simulation.SimulatedTail.Proxy;

namespace Brumba.VehicleBrains.Behaviours.OnGroundTailBehaviour
{
	[Contract(Contract.Identifier)]
//    [DisplayName("On Ground Tail Behaviour")]
    [Description("no description provided")]
	class OnGroundTailBehaviourService : DsspServiceBase
	{
        public class Calculator
        {
            public Vector2 Calculate(float steerAngle, float velocity)
            {
                return new Vector2(MathHelper.PiOver2, MathHelper.PiOver2);
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
        
        public OnGroundTailBehaviourService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}

		protected override void Start()
		{
		    base.Start();

            InitState();

		    SpawnIterator(Execute);
		}

	    [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnReplace(Replace replaceRequest)
        {
            _state = replaceRequest.Body;
            replaceRequest.ResponsePort.Post(DefaultReplaceResponseType.Instance);
        }

        IEnumerator<ITask> Execute()
        {
            while (true)
            {
                SimulatedAckermanFourWheelsState vehState = null;
                yield return Arbiter.Receive<SimulatedAckermanFourWheelsState>(false, _vehicle.Get(), st => vehState = st);

                var angles = new Calculator().Calculate(vehState.SteerAngle, vehState.MotorPower);

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
                    VehicleCoMY = 0.1f
	            };
	    }
	}
}