using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.AckermanVehicle.Proxy;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Ccr.Adapters.Wpf;

namespace Brumba.AckermanVehicleDriverGuiService
{
	[Contract(Contract.Identifier)]
	[DisplayName("AckermanVehicleDriverGuiService")]
	[Description("AckermanVehicleDriverGuiService service (no description provided)")]
	class AckermanVehicleDriverGuiService : DsspServiceBase
	{
		[ServiceState]
		AckermanVehicleDriverGuiServiceState _state = new AckermanVehicleDriverGuiServiceState();
		
		[ServicePort("/AckermanVehicleDriverGuiService", AllowMultipleInstances = true)]
		AckermanVehicleDriverGuiServiceOperations _mainPort = new AckermanVehicleDriverGuiServiceOperations();

        [Partner("Ackerman Vehicle", Contract = AckermanVehicle.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        AckermanVehicleOperations _ackermanVehPort = new AckermanVehicleOperations();

        MainWindowEvents _mainWindowEventsPort = new MainWindowEvents();
		
		public AckermanVehicleDriverGuiService(DsspServiceCreationPort creationPort)
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
            _ackermanVehPort.UpdateSteerAngle(new SteerAngle { Value = onSteerRequest.Direction * 1f });
        }

        private void OnPowerHandler(OnPower onPowerRequest)
        {
            _ackermanVehPort.UpdateDrivePower(new DrivePower { Value = onPowerRequest.Power * 1f });
        }

        private void OnBreakHandler(OnBreak onBreakRequest)
        {
            _ackermanVehPort.Break();
        }
    }
}


