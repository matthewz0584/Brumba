using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brumba.Common;
using Brumba.MapProvider;
using Brumba.McLrfLocalizer;
using Brumba.Utils;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Ccr.Adapters.Wpf;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Xna.Framework;
using DwaNavigatorPxy = Brumba.DwaNavigator.Proxy;
using McLrfLocalizerPxy = Brumba.McLrfLocalizer.Proxy;
using SimLocalizerPxy = Brumba.Simulation.SimulatedLocalizer.Proxy;
using CommonPxy = Brumba.Common.Proxy;
using OdometryPxy = Brumba.DiffDriveOdometry.Proxy;
using MapProviderPxy = Brumba.MapProvider.Proxy;
using W3C.Soap;

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

        //[Partner("DwaNavigator", Contract = DwaNavigatorPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        DwaNavigatorPxy.DwaNavigatorOperations _dwaNavigator = new DwaNavigatorPxy.DwaNavigatorOperations();

        //[Partner("McLrfLocalizer", Contract = McLrfLocalizerPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        McLrfLocalizerPxy.McLrfLocalizerOperations _mcLrfLocalizer = new McLrfLocalizerPxy.McLrfLocalizerOperations();

        //[Partner("Map", Contract = MapProviderPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        MapProviderPxy.MapProviderOperations _mapProvider = new MapProviderPxy.MapProviderOperations();

        //[Partner("SimulatedLocalizer", Contract = SimLocalizerPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SimLocalizerPxy.SimulatedLocalizerOperations _simLocalizer = new SimLocalizerPxy.SimulatedLocalizerOperations();

        //[Partner("Odometry", Contract = OdometryPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        OdometryPxy.DiffDriveOdometryOperations _odometry = new OdometryPxy.DiffDriveOdometryOperations();

        readonly Port<DateTime> _mapProviderPollingPort = new Port<DateTime>();
        readonly Port<DateTime> _dwaPollingPort = new Port<DateTime>();
        readonly Port<DateTime> _simLocalizerPollingPort = new Port<DateTime>();
        readonly Port<DateTime> _odometryPollingPort = new Port<DateTime>();
        readonly Port<DateTime> _mcLrfPollingPort = new Port<DateTime>();
	    
        WpfServicePort _wpfPort;
        MainWindow _mainWindow;

	    DwaNavigatorPxy.VelocityAcceleration _currentVelocityAcceleration;
	    CommonPxy.Pose _simulationPose;
        CommonPxy.Velocity _simulationVelocity;
        CommonPxy.Pose _odometryPose;
        CommonPxy.Velocity _odometryVelocity;
        CommonPxy.Pose _mcLrfLocalizerPose;
	    int _dwaIteration;

	    OccupancyGrid _map;
	    PoseHistogram _poseHistogram;

	    public DashboardService(DsspServiceCreationPort creationPort)
			: base(creationPort)
	    {
	        FillDummyData();
	    }

	    public IEnumerable<MatrixCell> DwaVelocitiesEvaluation { get; private set; }
        public IEnumerable<MatrixCell> DwaVelocitiesEvaluationMax { get; private set; }

        public IEnumerable<MatrixCell> McLrfParticlesHistogram { get; private set; }
        public IEnumerable<MatrixCell> McLrfParticlesHistogramMax { get; private set; }

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

	    public int DwaIteration
	    {
	        get { return _dwaIteration; }
	    }

	    public string SimulationPose
	    {
            get { return string.Format("Position: ({0:F2}, {1:F2}), Bearing: {2:F2}", _simulationPose.Position.X, _simulationPose.Position.Y, _simulationPose.Bearing); }
	    }

        public string SimulationVelocity
        {
            get { return string.Format("Linear: {0:F2} [m/s], Angular: {1:F2} [1/s]", _simulationVelocity.Linear, _simulationVelocity.Angular); }
        }

	    public string OdometryPose
	    {
            get { return string.Format("Position: ({0:F2}, {1:F2}), Bearing: {2:F2}", _odometryPose.Position.X, _odometryPose.Position.Y, _odometryPose.Bearing); }
	    }

        public string OdometryVelocity
        {
            get { return string.Format("Linear: {0:F2} [m/s], Angular: {1:F2} [1/s]", _odometryVelocity.Linear, _odometryVelocity.Angular); }
        }

        public string McLrfLocalizerPose
        {
            get { return string.Format("Position: ({0:F2}, {1:F2}), Bearing: {2:F2}", _mcLrfLocalizerPose.Position.X, _mcLrfLocalizerPose.Position.Y, _mcLrfLocalizerPose.Bearing); }
        }

	    protected override void Start()
		{
		    _wpfPort = WpfAdapter.Create(TaskQueue);
			base.Start();
            SpawnIterator(StartIt);
		}

        IEnumerator<ITask> StartIt()
        {
            var mclFoundPort = new Port<DateTime>();
            var exceptionPort = new Port<Exception>();

            Dispatcher.AddCausality(new Causality("StartIt", exceptionPort));

            SpawnIterator(() => FindPartnerSetAndSignalPolling(
                MapProviderPxy.Contract.Identifier, (MapProviderPxy.MapProviderOperations p) => _mapProvider = p, _mapProviderPollingPort));
            SpawnIterator(() => FindPartnerSetAndSignalPolling(
                McLrfLocalizerPxy.Contract.Identifier, (McLrfLocalizerPxy.McLrfLocalizerOperations p) => _mcLrfLocalizer = p, mclFoundPort));
            SpawnIterator(() => FindPartnerSetAndSignalPolling(
                DwaNavigatorPxy.Contract.Identifier, (DwaNavigatorPxy.DwaNavigatorOperations p) => _dwaNavigator = p, _dwaPollingPort));
            SpawnIterator(() => FindPartnerSetAndSignalPolling(
                SimLocalizerPxy.Contract.Identifier, (SimLocalizerPxy.SimulatedLocalizerOperations p) => _simLocalizer = p, _simLocalizerPollingPort));
            SpawnIterator(() => FindPartnerSetAndSignalPolling(
                OdometryPxy.Contract.Identifier, (OdometryPxy.DiffDriveOdometryOperations p) => _odometry = p, _odometryPollingPort));

            Activate(Arbiter.JoinedReceive(false, mclFoundPort, _mapReadyPort, (_, __) => _mcLrfPollingPort.Post(DateTime.Now)));

            MainPortInterleave.CombineWith(new Interleave(
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.ReceiveWithIterator(true, _mapProviderPollingPort, UpdateMap),
                    Arbiter.ReceiveWithIterator(true, _dwaPollingPort, UpdateDwaVelocitiesEvaluation),
                    Arbiter.ReceiveWithIterator(true, _simLocalizerPollingPort, UpdateSimulation),
                    Arbiter.ReceiveWithIterator(true, _odometryPollingPort, UpdateOdometry),
                    Arbiter.ReceiveWithIterator(true, _mcLrfPollingPort, UpdateMcLocalization),
                    Arbiter.Receive(true, exceptionPort, Console.WriteLine)
                    )));
            //Problem: service can not be dropped if wpf window has been closed before (and it could easily be by user).
            //If so _wpfPort ceases to process messages, polling handlers' receivers wait on them forever,
            //and, since these handlers are called from inside of ConcurrentGroup, TearDownGroup with Drop handler could not be started.
            //Adding timeout to _wpfPort.Invoke choice fixes the problem, but it is soo clumsy.
            //Similar problem could appear in general in different contexts:
            //service whose ConcurrentGroup level handler has hanged could not be dropped.

            yield return _wpfPort.RunWindow(() => new MainWindow(this)).Choice(
                w => _mainWindow = w as MainWindow, LogError);
        }

        readonly Port<DateTime> _mapReadyPort = new Port<DateTime>();
        IEnumerator<ITask> UpdateMap(DateTime dateTime)
        {
            yield return _mapProvider.Get().Receive(s => _map = (OccupancyGrid)DssTypeHelper.TransformFromProxy(s.Map));
            _map.Freeze();

            _poseHistogram = new PoseHistogram(_map, Math.PI / 18);

            _mapReadyPort.Post(DateTime.Now);
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
            _dwaIteration = dwaState.Iteration;

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
                        PropertyChanged(this, new PropertyChangedEventArgs("DwaIteration"));
                    }).Choice(success => {}, LogError);

            Activate(Arbiter.Receive(false, TimeoutPort(500), t => _dwaPollingPort.Post(t)));
        }

        IEnumerator<ITask> UpdateMcLocalization(DateTime dateTime)
        {
            McLrfLocalizerPxy.McLrfLocalizerState mcLrfState = null;
            yield return _mcLrfLocalizer.Get().Receive(ds => mcLrfState = ds);

            if (mcLrfState.Particles == null)
            {
                Activate(Arbiter.Receive(false, TimeoutPort(1000), t => _mcLrfPollingPort.Post(t)));
                yield break;
            }

            _poseHistogram.Build(mcLrfState.Particles.Select(px => (Pose)DssTypeHelper.TransformFromProxy(px)));
            var hist = _poseHistogram.ToXyMarginal();

            var cells = new List<MatrixCell>();
            for (var y = 0; y < _map.SizeInCells.Y; ++y)
                for (var x = 0; x < _map.SizeInCells.X; ++x)
                    cells.Add(new MatrixCell { Row = y, Col = x, Value = _map[x, y] ? -1 : hist[x, y] });

            McLrfParticlesHistogram = cells;
            McLrfParticlesHistogramMax = McLrfParticlesHistogram.OrderByDescending(mc => mc.Value).First().AsCol().ToList();
            _mcLrfLocalizerPose = mcLrfState.EstimatedPose;

            yield return _wpfPort.Invoke(() =>
            {
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("McLrfParticlesHistogram"));
                    PropertyChanged(this, new PropertyChangedEventArgs("McLrfParticlesHistogramMax"));
                    PropertyChanged(this, new PropertyChangedEventArgs("McLrfLocalizerPose"));
                }
                catch (Exception)
                { }
            }).Choice(success => { }, LogError);

            Activate(Arbiter.Receive(false, TimeoutPort(1000), t => _mcLrfPollingPort.Post(t)));
        }

        IEnumerator<ITask> UpdateSimulation(DateTime dateTime)
        {
            SimLocalizerPxy.SimulatedLocalizerState localizerState = null;
            yield return _simLocalizer.Get().Receive(ls => localizerState = ls);

            _simulationPose = localizerState.Localizer.EstimatedPose;
            _simulationVelocity = localizerState.FixedWheelVelocimeter.Velocity;

            yield return _wpfPort.Invoke(() =>
            {
                PropertyChanged(this, new PropertyChangedEventArgs("SimulationPose"));
                PropertyChanged(this, new PropertyChangedEventArgs("SimulationVelocity"));
            }).Choice(success => { }, LogError);

            Activate(Arbiter.Receive(false, TimeoutPort(100), t => _simLocalizerPollingPort.Post(t)));
        }

        IEnumerator<ITask> UpdateOdometry(DateTime dateTime)
        {
            OdometryPxy.DiffDriveOdometryServiceState odometryState = null;
            yield return _odometry.Get().Receive(os => odometryState = os);

            _odometryPose = odometryState.Pose;
            _odometryVelocity = odometryState.Velocity;

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
            DwaVelocitiesEvaluationMax = new List<MatrixCell>
	        {
	            new MatrixCell {Row = 0, Col = 0, Value = 1},
	        };
            _currentVelocityAcceleration = new DwaNavigatorPxy.VelocityAcceleration
            {
                Velocity = new CommonPxy.Velocity
                {
                    Linear = 5,
                    Angular = 5.5
                },
                WheelAcceleration = new Vector2(0.5f, 0.6f)
            };
            _simulationPose = new CommonPxy.Pose(new Vector2(1, 2), Math.PI);
            _simulationVelocity = new CommonPxy.Velocity(3, Math.PI);
            _odometryPose = new CommonPxy.Pose(new Vector2(10, 20), Math.PI);
            _odometryVelocity = new CommonPxy.Velocity(30, Math.PI);
            _mcLrfLocalizerPose = new CommonPxy.Pose(new Vector2(100, 200), Math.PI);

            McLrfParticlesHistogram = new List<MatrixCell>
            {
	            new MatrixCell {Row = 0, Col = 0, Value = -1},
	            new MatrixCell {Row = 0, Col = 1, Value = -1},
	            new MatrixCell {Row = 0, Col = 2, Value = -1},
	            new MatrixCell {Row = 1, Col = 0, Value = -1},
	            new MatrixCell {Row = 1, Col = 1, Value = 100},
	            new MatrixCell {Row = 1, Col = 2, Value = -1},
	            new MatrixCell {Row = 2, Col = 0, Value = -1},
	            new MatrixCell {Row = 2, Col = 1, Value = -1},
	            new MatrixCell {Row = 2, Col = 2, Value = -1}
	        };
            McLrfParticlesHistogramMax = new List<MatrixCell>
            {
	            new MatrixCell {Row = 1, Col = 1, Value = 100},
	        };
        }

	    IEnumerator<ITask> FindPartnerSetAndSignalPolling<T>(string identifier, Action<T> setPartner, Port<DateTime> partnerPollingPort) where T : class, IPortSet, new()
	    {
            var directoryResponcePort = DirectoryQuery(identifier, new TimeSpan(0, 0, 2));

            T partner = null;
            yield return Arbiter.Choice(
                TimeoutPort(2000).Receive(timeout => partner = null),
                directoryResponcePort.Receive((Fault fault) => partner = null),
                directoryResponcePort.Receive(serviceInfo => partner = ServiceForwarder<T>(new Uri(serviceInfo.Service))));

            if (partner == null)
                yield break;

	        setPartner(partner);
            partnerPollingPort.Post(DateTime.Now);
	    }

        //IEnumerator<ITask> FindPartner<T>(Action<T> @return, string identifier) where T : class, IPortSet, new()
        //{
        //    var directoryResponcePort = DirectoryQuery(identifier, new TimeSpan(0, 0, 2));

        //    yield return Arbiter.Choice(
        //        TimeoutPort(2000).Receive(timeout => @return(null)),
        //        directoryResponcePort.Receive((Fault fault) => @return(null)),
        //        directoryResponcePort.Receive(serviceInfo => @return(ServiceForwarder<T>(new Uri(serviceInfo.Service)))));
        //}
	}

    public struct MatrixCell
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public double Value { get; set; }
    }
}