using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Ccr.Adapters.Wpf;
using Brumba.Simulation.SimulatedAckermanVehicleEx.Proxy;

namespace Brumba.Simulation.AckermanFourWheelsDriverGuiService
{
	[Contract(Contract.Identifier)]
	[DisplayName("AckermanFourWheelsDriverGuiService")]
	[Description("AckermanFourWheelsDriverGuiService service (no description provided)")]
	class AckermanFourWheelsDriverGuiService : DsspServiceBase
	{
		[ServiceState]
		AckermanFourWheelsDriverGuiServiceState _state = new AckermanFourWheelsDriverGuiServiceState();
		
		[ServicePort("/AckermanFourWheelsDriverGuiService", AllowMultipleInstances = true)]
		AckermanFourWheelsDriverGuiServiceOperations _mainPort = new AckermanFourWheelsDriverGuiServiceOperations();

        [Partner("Simulated Ackerman Four Wheels", Contract = SimulatedAckermanVehicleEx.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SimulatedAckermanVehicleExOperations _simFourWheels = new SimulatedAckermanVehicleExOperations();

        MainWindowEvents _mainWindowEventsPort = new MainWindowEvents();
		
		public AckermanFourWheelsDriverGuiService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}
		
		protected override void Start()
		{
            SpawnIterator(StartGui);
			base.Start();
		}

        private IEnumerator<ITask> StartGui()
        {
            var runWndResponse = WpfAdapter.Create(TaskQueue).RunWindow(() => new MainWindow(_mainWindowEventsPort));
            yield return (Choice)runWndResponse;

            if ((Exception)runWndResponse != null)
            {
                LogError(runWndResponse);
                yield break;
            }

            MainPortInterleave.CombineWith(new Interleave(
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<OnSteer>(true, _mainWindowEventsPort, OnSteerHandler),
                    Arbiter.Receive<OnPower>(true, _mainWindowEventsPort, OnPowerHandler),
                    Arbiter.Receive<OnBreak>(true, _mainWindowEventsPort, OnBreakHandler)
                    )));
        }

        private void OnSteerHandler(OnSteer onSteerRequest)
        {
            //_simFourWheels.SetSteerAngle(new SteerAngleRequest { Value = onSteerRequest.Direction * 0.2f });
            _simFourWheels.SetSteerAngle(new SteerAngleRequest { Value = onSteerRequest.Direction * 1f });
        }

        private void OnPowerHandler(OnPower onPowerRequest)
        {
            //_simFourWheels.SetMotorPower(new MotorPowerRequest { Value = onPowerRequest.Direction * 0.2f });
            _simFourWheels.SetMotorPower(new MotorPowerRequest { Value = onPowerRequest.Direction * 1f });
        }

        private void OnBreakHandler(OnBreak onBreakRequest)
        {
            _simFourWheels.Break();
        }
    }
}


