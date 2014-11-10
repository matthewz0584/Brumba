using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Adapters.Wpf;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;

namespace Brumba.Dashboard
{
	[Contract(Contract.Identifier)]
    [DisplayName("Brumba Dashboard")]
	[Description("")]
	public class DashboardService : DsspServiceBase, INotifyPropertyChanged
	{
        public event PropertyChangedEventHandler PropertyChanged = delegate {};

		[ServiceState]
		DashboardState _state = new DashboardState();
		
		[ServicePort("/DashboardService", AllowMultipleInstances = true)]
		DashboardOperations _mainPort = new DashboardOperations();

        //[Partner("Vehicle", Contract = AckermanVehicle.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        //AckermanVehicleOperations _ackermanVehPort = new AckermanVehicleOperations();

        //[Partner("Turret", Contract = Simulation.SimulatedTurret.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        //SimulatedTurretOperations _turretPort = new SimulatedTurretOperations();

        //[Partner("Camera", Contract = Microsoft.Robotics.Services.WebCam.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        //WebCamOperations _cameraPort = new WebCamOperations();
        //WebCamOperations _cameraNotificationPort = new WebCamOperations();

        //[Partner("Rf ring", Contract = Simulation.SimulatedInfraredRfRing.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        //SimulatedInfraredRfRingOperations _rfRingPort = new SimulatedInfraredRfRingOperations();

	    readonly Port<DateTime> _dwaPollingPort = new Port<DateTime>();
	    
        WpfServicePort _wpfPort;
        MainWindow _mainWindow;

	    public DashboardService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}

        public IEnumerable<MatrixCell> DwaVelocitiesEvaluation { get; private set; }

		protected override void Start()
		{
		    _wpfPort = WpfAdapter.Create(TaskQueue);
            SpawnIterator(StartGui);
			base.Start();
		}

        IEnumerator<ITask> StartGui()
        {
            yield return _wpfPort.RunWindow(() => new MainWindow(this)).Choice(
                w => _mainWindow = w as MainWindow, LogError);

            MainPortInterleave.CombineWith(new Interleave(
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.ReceiveWithIterator(true, _dwaPollingPort, UpdateDwaVelocitiesEvaluation)
                    )));
            _dwaPollingPort.Post(DateTime.Now);
        }

        IEnumerator<ITask> UpdateDwaVelocitiesEvaluation(DateTime dateTime)
        {
            //get dwa evaluations
            DwaVelocitiesEvaluation = new List<MatrixCell>
            {
                new MatrixCell { Row = 0, Col = 0, Value = 1}, new MatrixCell { Row = 0, Col = 1, Value = 0}, new MatrixCell { Row = 0, Col = 2, Value = 1},
                new MatrixCell { Row = 1, Col = 0, Value = 0}, new MatrixCell { Row = 1, Col = 1, Value = 1}, new MatrixCell { Row = 1, Col = 2, Value = 0},
                new MatrixCell { Row = 2, Col = 0, Value = 1}, new MatrixCell { Row = 2, Col = 1, Value = 0}, new MatrixCell { Row = 2, Col = 2, Value = 1}
            };

            yield return _wpfPort.Invoke(() => PropertyChanged(this, new PropertyChangedEventArgs("DwaVelocitiesEvaluation"))).Choice(
                success => {}, LogError);

            Activate(Arbiter.Receive(false, TimeoutPort(500), t => _dwaPollingPort.Post(t)));
        }
	}

    public struct MatrixCell
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public double Value { get; set; }
    }
}


