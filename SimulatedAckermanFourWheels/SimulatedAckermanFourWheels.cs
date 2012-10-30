using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;
using Microsoft.Robotics.Simulation.Engine;
using System.Diagnostics;

namespace Brumba.Simulation.SimulatedAckermanFourWheels
{
	[Contract(Contract.Identifier)]
	[DisplayName("SimulatedAckermanFourWheels")]
	[Description("SimulatedAckermanFourWheels service (no description provided)")]
	class SimulatedAckermanFourWheelsService : DsspServiceBase
	{
		[ServiceState]
		private SimulatedAckermanFourWheelsState _state = new SimulatedAckermanFourWheelsState();
		
		[ServicePort("/SimulatedAckermanFourWheels", AllowMultipleInstances = true)]
		private SimulatedAckermanFourWheelsOperations _mainPort = new SimulatedAckermanFourWheelsOperations();

        private SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        private AckermanFourWheelsEntity _vehicle;
		
		public SimulatedAckermanFourWheelsService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}

		protected override void Start()
		{
            SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _simEngineNotifyPort);

            SetUpForWaitingForEntity();
            base.Start();
		}

        private void OnInsertEntity(InsertSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanFourWheels OnInsertEntity called");
            
            _vehicle = entity.Body as AckermanFourWheelsEntity;
            _vehicle.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        private void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanFourWheels OnDeleteEntity called");
            
            _vehicle = null;
            _state.Connected = false;
            _state.MotorPower = 0;
            _state.SteerAngle = 0;

            SetUpForWaitingForEntity();
        }

        private void OnSetMotorPower(SetMotorPower motorRequest)
        {
            _state.MotorPower = motorRequest.Body.Value;
            _vehicle.SetMotorPower(_state.MotorPower);

            motorRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        private void OnSetSteerAngle(SetSteerAngle steerRequest)
        {
            _state.SteerAngle = steerRequest.Body.Value;
            _vehicle.SetSteerAngle(_state.SteerAngle);

            steerRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        private void OnBreak(Break breakRequest)
        {
            _state.MotorPower = 0;
            _vehicle.Break();

            breakRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        private void SetUpForWaitingForEntity()
        {
            Activate(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                        Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)
                        ),
                    new ExclusiveReceiverGroup(),
                    new ConcurrentReceiverGroup()));
        }

        private void SetUpForControlOfEntity()
        {
            Activate(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                        Arbiter.Receive<DeleteSimulationEntity>(false, _simEngineNotifyPort, OnDeleteEntity)
                        ),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<SetMotorPower>(true, _mainPort, OnSetMotorPower),
                        Arbiter.Receive<SetSteerAngle>(true, _mainPort, OnSetSteerAngle),
                        Arbiter.Receive<Break>(true, _mainPort, OnBreak)
                        ),
                    new ConcurrentReceiverGroup()));
        }
	}
}


