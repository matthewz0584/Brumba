using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brumba.Utils;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Ccr.Adapters.Wpf;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using DwaNavigatorPxy = Brumba.DwaNavigator.Proxy;
using LocalizerPxy = Brumba.GenericLocalizer.Proxy;
using VelocimeterPxy = Brumba.GenericVelocimeter.Proxy;
using WaiterPxy = Brumba.WaiterStupid.Proxy;
using OdometryPxy = Brumba.DiffDriveOdometry.Proxy;

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

        [Partner("DwaNavigator", Contract = DwaNavigatorPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        DwaNavigatorPxy.DwaNavigatorOperations _dwaNavigator = new DwaNavigatorPxy.DwaNavigatorOperations();

        [Partner("Localizer", Contract = LocalizerPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        LocalizerPxy.GenericLocalizerOperations _localizer = new LocalizerPxy.GenericLocalizerOperations();

        [Partner("Velocimeter", Contract = VelocimeterPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        VelocimeterPxy.GenericVelocimeterOperations _velocimeter = new VelocimeterPxy.GenericVelocimeterOperations();

        //[Partner("Odometry", Contract = OdometryPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        //OdometryPxy.DiffDriveOdometryOperations _odometry = new OdometryPxy.DiffDriveOdometryOperations();

	    readonly Port<DateTime> _dwaPollingPort = new Port<DateTime>();
        readonly Port<DateTime> _simulationPollingPort = new Port<DateTime>();
        readonly Port<DateTime> _odometryPollingPort = new Port<DateTime>();
	    
        WpfServicePort _wpfPort;
        MainWindow _mainWindow;

	    private DwaNavigatorPxy.VelocityAcceleration _currentVelocityAcceleration;
	    private WaiterPxy.Pose _simulationPose;
        private WaiterPxy.Pose _simulationVelocity;
        private WaiterPxy.Pose _odometryPose;
        private WaiterPxy.Pose _odometryVelocity;
	    private int _currentDwaIteration;

	    public DashboardService(DsspServiceCreationPort creationPort)
			: base(creationPort)
	    {
	        FillDummyData();
	    }

	    public IEnumerable<MatrixCell> DwaVelocitiesEvaluation { get; private set; }
        public IEnumerable<MatrixCell> DwaVelocitiesEvaluationMax { get; private set; }

	    public string CurrentVelocity
	    {
	        get
	        {
                return string.Format("Linear: {0:F2} [m/s], Angular: {1:F2} [1/s]",
                    _currentVelocityAcceleration.Velocity.Linear, _currentVelocityAcceleration.Velocity.Angular);
	        }
	    }

        public string CurrentWheelAcceleration
        {
            get
            {
                return string.Format("Left: {0:F1} [1/s2], Right: {1:F1} [1/s2]",
                    _currentVelocityAcceleration.WheelAcceleration.X, _currentVelocityAcceleration.WheelAcceleration.Y);
            }
        }

	    public int CurrentDwaIteration
	    {
	        get { return _currentDwaIteration; }
	    }

	    public string SimulationPose
	    {
            get { return string.Format("Position: ({0:F2}, {1:F2}), Bearing: {2:F2}", _simulationPose.Position.X, _simulationPose.Position.Y, _simulationPose.Bearing); }
	    }

        public string SimulationVelocity
        {
            get { return string.Format("Linear: ({0:F2}, {1:F2}), Angular: {2:F2}", _simulationVelocity.Position.X, _simulationVelocity.Position.Y, _simulationVelocity.Bearing); }
        }

	    public string OdometryPose
	    {
            get { return string.Format("Position: ({0:F2}, {1:F2}), Bearing: {2:F2}", _odometryPose.Position.X, _odometryPose.Position.Y, _odometryPose.Bearing); }
	    }

        public string OdometryVelocity
        {
            get { return string.Format("Linear: ({0:F2}, {1:F2}), Angular: {2:F2}", _odometryVelocity.Position.X, _odometryVelocity.Position.Y, _odometryVelocity.Bearing); }
        }

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
                    Arbiter.ReceiveWithIterator(true, _dwaPollingPort, UpdateDwaVelocitiesEvaluation),
                    Arbiter.ReceiveWithIterator(true, _simulationPollingPort, UpdateSimulation),
                    Arbiter.ReceiveWithIterator(true, _odometryPollingPort, UpdateOdometry)
                    )));
            _dwaPollingPort.Post(DateTime.Now);
            _simulationPollingPort.Post(DateTime.Now);
            //_odometryPollingPort.Post(DateTime.Now);
        }

        IEnumerator<ITask> UpdateDwaVelocitiesEvaluation(DateTime dateTime)
        {
            DwaNavigatorPxy.DwaNavigatorState dwaState = null;
            yield return _dwaNavigator.Get().Receive(ds => dwaState = ds);

            DwaVelocitiesEvaluation =
                DenseMatrix.OfArray(dwaState.VelocititesEvaluation).IndexedEnumerator().
                    Select(c => new MatrixCell
                            {
                                Row = c.Item1 - dwaState.VelocititesEvaluation.GetLength(0) / 2,
                                Col = c.Item2 - dwaState.VelocititesEvaluation.GetLength(1) / 2,
                                Value = c.Item3 <= -1 ? -1 : c.Item3
                            }).ToList();
            DwaVelocitiesEvaluationMax = DwaVelocitiesEvaluation.OrderByDescending(mc => mc.Value).First().AsCol().ToList();
            _currentVelocityAcceleration = dwaState.CurrentVelocityAcceleration;
            _currentDwaIteration = dwaState.Iteration;

            yield return _wpfPort.Invoke(() =>
                    {
                        try
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs("DwaVelocitiesEvaluation"));
                            PropertyChanged(this, new PropertyChangedEventArgs("DwaVelocitiesEvaluationMax"));
                        }
                        catch (Exception)
                        {}
                        PropertyChanged(this, new PropertyChangedEventArgs("CurrentVelocity"));
                        PropertyChanged(this, new PropertyChangedEventArgs("CurrentWheelAcceleration"));
                        PropertyChanged(this, new PropertyChangedEventArgs("CurrentDwaIteration"));
                    }).Choice(success => {}, LogError);

            Activate(Arbiter.Receive(false, TimeoutPort(500), t => _dwaPollingPort.Post(t)));
        }

        IEnumerator<ITask> UpdateSimulation(DateTime dateTime)
        {
            LocalizerPxy.GenericLocalizerState localizerState = null;
            yield return _localizer.Get().Receive(ls => localizerState = ls);

            _simulationPose = localizerState.EstimatedPose;

            VelocimeterPxy.GenericVelocimeterState velocimeterState = null;
            yield return _velocimeter.Get().Receive(vs => velocimeterState = vs);

            _simulationVelocity = velocimeterState.Velocity;

            yield return _wpfPort.Invoke(() =>
            {
                PropertyChanged(this, new PropertyChangedEventArgs("SimulationPose"));
                PropertyChanged(this, new PropertyChangedEventArgs("SimulationVelocity"));
            }).Choice(success => { }, LogError);

            Activate(Arbiter.Receive(false, TimeoutPort(100), t => _simulationPollingPort.Post(t)));
        }

        IEnumerator<ITask> UpdateOdometry(DateTime dateTime)
        {
            OdometryPxy.DiffDriveOdometryServiceState odometryState = null;
            //yield return _odometry.Get().Receive(os => odometryState = os);

            _odometryPose = odometryState.State.Pose;
            _odometryVelocity = odometryState.State.Velocity;

            yield return _wpfPort.Invoke(() =>
            {
                PropertyChanged(this, new PropertyChangedEventArgs("OdometryPose"));
                PropertyChanged(this, new PropertyChangedEventArgs("OdometryVelocity"));
            }).Choice(success => { }, LogError);

            Activate(Arbiter.Receive(false, TimeoutPort(100), t => _odometryPollingPort.Post(t)));
        }

        void FillDummyData()
        {
            DwaVelocitiesEvaluation = new List<MatrixCell>
	        {
	            new MatrixCell {Row = 0, Col = 0, Value = 1},
	            new MatrixCell {Row = 0, Col = 1, Value = 0},
	            new MatrixCell {Row = 0, Col = 2, Value = 1},
	            new MatrixCell {Row = 1, Col = 0, Value = 0},
	            new MatrixCell {Row = 1, Col = 1, Value = 1},
	            new MatrixCell {Row = 1, Col = 2, Value = 0},
	            new MatrixCell {Row = 2, Col = 0, Value = 1},
	            new MatrixCell {Row = 2, Col = 1, Value = 0},
	            new MatrixCell {Row = 2, Col = 2, Value = 1}
	        };
            _currentVelocityAcceleration = new DwaNavigatorPxy.VelocityAcceleration
            {
                Velocity = new DwaNavigatorPxy.Velocity
                {
                    Linear = 5,
                    Angular = 5.5
                },
                WheelAcceleration = new Vector2(0.5f, 0.6f)
            };
            _simulationPose = new WaiterPxy.Pose(new Vector2(1, 2), Math.PI);
            _simulationVelocity = new WaiterPxy.Pose(new Vector2(101, 201), Math.PI);
            _odometryPose = new WaiterPxy.Pose(new Vector2(10, 20), Math.PI);
            _odometryVelocity = new WaiterPxy.Pose(new Vector2(100, 200), Math.PI);
        }
	}

    public struct MatrixCell
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public double Value { get; set; }
    }
}


