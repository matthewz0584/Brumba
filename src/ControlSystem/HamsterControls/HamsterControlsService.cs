using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.AckermanVehicle.Proxy;
using Brumba.Simulation.SimulatedInfraredRfRing.Proxy;
using Brumba.Simulation.SimulatedTurret.Proxy;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Ccr.Adapters.Wpf;
using Microsoft.Robotics.Services.WebCam.Proxy;

namespace Brumba.HamsterControls
{
	[Contract(Contract.Identifier)]
	[DisplayName("HamsterControlsService")]
	[Description("HamsterControlsService service (no description provided)")]
	class HamsterControlsService : DsspServiceBase
	{
        int VIEW_SIZE_LENGTH = 320;
        int VIEW_SIZE_HEIGHT = 240;

		[ServiceState]
		HamsterControlsState _state = new HamsterControlsState();
		
		[ServicePort("/HamsterControlsService", AllowMultipleInstances = true)]
		HamsterControlsOperations _mainPort = new HamsterControlsOperations();

        [Partner("Vehicle", Contract = AckermanVehicle.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        AckermanVehicleOperations _ackermanVehPort = new AckermanVehicleOperations();

        [Partner("Turret", Contract = Simulation.SimulatedTurret.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SimulatedTurretOperations _turretPort = new SimulatedTurretOperations();

        [Partner("Camera", Contract = Microsoft.Robotics.Services.WebCam.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        WebCamOperations _cameraPort = new WebCamOperations();
        WebCamOperations _cameraNotificationPort = new WebCamOperations();

        [Partner("Rf ring", Contract = Simulation.SimulatedInfraredRfRing.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SimulatedInfraredRfRingOperations _rfRingPort = new SimulatedInfraredRfRingOperations();

        MainWindowEvents _mainWindowEventsPort = new MainWindowEvents();
        Port<DateTime> _rfPollingTimerPort = new Port<DateTime>();
	    
        WpfServicePort _wpfPort;
        MainWindow _mainWindow;

	    public HamsterControlsService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}
		
		protected override void Start()
		{
		    _wpfPort = WpfAdapter.Create(TaskQueue);
            SpawnIterator(StartGui);
			base.Start();
		}

        IEnumerator<ITask> StartGui()
        {
            var runWndResponse = _wpfPort.RunWindow(() => new MainWindow(_mainWindowEventsPort));
            yield return (Choice)runWndResponse;

            _mainWindow = (MainWindow) runWndResponse;

            var cameraSubscrResponse = _cameraPort.Subscribe(_cameraNotificationPort);
            yield return (Choice)cameraSubscrResponse;

            MainPortInterleave.CombineWith(new Interleave(
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<SteerRequest>(true, _mainWindowEventsPort, OnSteerHandler),
                    Arbiter.Receive<PowerRequest>(true, _mainWindowEventsPort, OnPowerHandler),
                    Arbiter.Receive<BreakRequest>(true, _mainWindowEventsPort, OnBreakHandler),
                    Arbiter.Receive<TurretBaseAngleRequest>(true, _mainWindowEventsPort, OnTurretBaseAngleRequest),
                    Arbiter.ReceiveWithIterator<UpdateFrame>(true, _cameraNotificationPort, OnNewCameraFrame),
                    Arbiter.ReceiveWithIterator(true, _rfPollingTimerPort, PollRfRing)
                    )));
            _rfPollingTimerPort.Post(DateTime.Now);
        }

	    IEnumerator<ITask> PollRfRing(DateTime dateTime)
        {
            var rfRingGetResponse = _rfRingPort.Get();
            yield return (Choice)rfRingGetResponse;

            yield return (Choice)_wpfPort.Invoke(() => _mainWindow.Vm.UpdateIrRfRing(((SimulatedInfraredRfRingState)rfRingGetResponse).Distances));

            Activate(Arbiter.Receive(false, TimeoutPort(100), t => _rfPollingTimerPort.Post(t)));
        }

	    IEnumerator<ITask> OnNewCameraFrame(UpdateFrame updateFrameRequest)
	    {
	        var queryFrameResponse = _cameraPort.QueryFrame();
            yield return (Choice)(queryFrameResponse);

            yield return (Choice)_wpfPort.Invoke(() => _mainWindow.Vm.UpdateCameraFrame(((QueryFrameResponse)queryFrameResponse).Frame, VIEW_SIZE_LENGTH, VIEW_SIZE_HEIGHT));
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


