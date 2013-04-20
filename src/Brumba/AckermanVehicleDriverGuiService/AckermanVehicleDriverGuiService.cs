using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.AckermanVehicle.Proxy;
using Brumba.Simulation.SimulatedTurret.Proxy;
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

        [Partner("Camera Turret", Contract = Simulation.SimulatedTurret.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SimulatedTurretOperations _turretPort = new SimulatedTurretOperations();

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
                    Arbiter.Receive<SteerRequest>(true, _mainWindowEventsPort, OnSteerHandler),
                    Arbiter.Receive<PowerRequest>(true, _mainWindowEventsPort, OnPowerHandler),
                    Arbiter.Receive<BreakRequest>(true, _mainWindowEventsPort, OnBreakHandler),
                    Arbiter.Receive<TurretBaseAngleRequest>(true, _mainWindowEventsPort, OnTurretBaseAngleRequest)
                    )));
        }

        void OnSteerHandler(SteerRequest onSteerRequest)
        {
            _ackermanVehPort.UpdateSteeringAngle(onSteerRequest.Value);
        }

        void OnPowerHandler(PowerRequest onPowerRequest)
        {
            _ackermanVehPort.UpdateDrivePower(onPowerRequest.Value);
        }

        void OnBreakHandler(BreakRequest onBreakRequest)
        {
            _ackermanVehPort.Break();
        }

        void OnTurretBaseAngleRequest(TurretBaseAngleRequest onTurretBaseRequest)
        {
            _turretPort.SetBaseAngle(onTurretBaseRequest.Value);
        }
    }
}


