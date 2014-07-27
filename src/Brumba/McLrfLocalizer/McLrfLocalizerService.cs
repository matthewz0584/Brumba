using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Brumba.DsspUtils;
using Brumba.MapProvider;
using Brumba.WaiterStupid;
using Brumba.WaiterStupid.GUI;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Services.SubscriptionManager;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;
using SickLrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;
using OdometryPxy = Brumba.DiffDriveOdometry.Proxy;
using MapProviderPxy = Brumba.MapProvider.Proxy;

namespace Brumba.McLrfLocalizer
{
    [Contract(Contract.Identifier)]
    [DisplayName("Brumba MonteCarlo Localizer")]
    [Description("no description provided")]
    public class McLrfLocalizerService : DsspServiceExposing
	{
#pragma warning disable 0649
		[ServiceState]
		[InitialStatePartner(Optional = false)]
		McLrfLocalizerState _state;
#pragma warning restore 0649

        [ServicePort("/McLrfLocalizer", AllowMultipleInstances = true)]
        McLrfLocalizerOperations _mainPort = new McLrfLocalizerOperations();

        [Partner("Odometry", Contract = OdometryPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        OdometryPxy.DiffDriveOdometryOperations _odometryProvider = new OdometryPxy.DiffDriveOdometryOperations();

        [Partner("Lrf", Contract = SickLrfPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SickLrfPxy.SickLRFOperations _lrf = new SickLrfPxy.SickLRFOperations();

		[Partner("Map", Contract = MapProviderPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		MapProviderPxy.MapProviderOperations _mapProvider = new MapProviderPxy.MapProviderOperations();

		[SubscriptionManagerPartner("SubMgr")]
		SubscriptionManagerPort _subMgrPort = new SubscriptionManagerPort();

        McLrfLocalizer _localizer;
        TimerFacade _timerFacade;
        Pose _currentOdometry;

	    public McLrfLocalizerService(DsspServiceCreationPort creationPort)
		    : base(creationPort)
	    {
			DC.Contract.Requires(creationPort != null);
	    }

        protected override void Start()
        {
            SpawnIterator(StartIt);
        }

	    IEnumerator<ITask> StartIt()
	    {
			_timerFacade = new TimerFacade(this, _state.DeltaT);

			OccupancyGrid map = null;
		    yield return _mapProvider.Get().Receive(ms => map = (OccupancyGrid) DssTypeHelper.TransformFromProxy(ms.Map));
			map.Freeze();

			_localizer = new McLrfLocalizer(map, _state.RangeFinderProperties, _state.ParticlesNumber);

			if (IsPoseUnknown(_state.FirstPoseCandidate))
				_localizer.InitPoseUnknown();
			else
				_localizer.InitPose(_state.FirstPoseCandidate, new Pose(new Vector2(0.3f, 0.3f), 10 * Constants.Degree));

		    yield return _odometryProvider.Get().Receive(os => _currentOdometry = (Pose) DssTypeHelper.TransformFromProxy(os.State.Pose));

			base.Start();

			MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
					Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateLocalizer))));

            yield return To.Exec(() => _timerFacade.Set());

			//************************************
            _mv.InitOnServiceStart(TaskQueue);
            _mv.InitVisual("qq", System.Windows.Media.Colors.White, System.Windows.Media.Colors.Black);
            yield return To.Exec(() => _mv.StartGui());
            yield return To.Exec(Draw);
        }

		IEnumerator<ITask> UpdateLocalizer(TimeSpan dt)
		{
			yield return Arbiter.JoinedReceive(false, _lrf.Get().P0, _odometryProvider.Get().P0, 
                (lrfScan, odometry) =>
                    {
						DC.Contract.Requires(lrfScan != null);
						DC.Contract.Requires(lrfScan.DistanceMeasurements != null);
						DC.Contract.Requires(odometry != null);
						DC.Contract.Requires(odometry.State != null);

                        var sw = Stopwatch.StartNew();

	                    var newOdometry = (Pose)DssTypeHelper.TransformFromProxy(odometry.State.Pose);
	                    _localizer.Update(newOdometry - _currentOdometry, lrfScan.DistanceMeasurements.Select(d => d / 1000f));
						_currentOdometry = newOdometry;
	                    
						UpdateState();
						LogInfo("loc {0} for {1}", _state.FirstPoseCandidate, sw.Elapsed);
                    });

			yield return To.Exec(Draw);
		}

	    [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public void OnQueryPose(QueryPose queryPoseRq)
		{
			DC.Contract.Requires(queryPoseRq != null);
			DC.Contract.Requires(queryPoseRq.Body != null);
			DC.Contract.Requires(queryPoseRq.ResponsePort != null);

			queryPoseRq.ResponsePort.Post(_state.FirstPoseCandidate);
		}

		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public void InitPoseRequest(InitPose initPoseRq)
		{
			DC.Contract.Requires(initPoseRq != null);
			DC.Contract.Requires(initPoseRq.Body != null);
			DC.Contract.Requires(initPoseRq.ResponsePort != null);

			_localizer.InitPose(initPoseRq.Body.Pose, new Pose(new Vector2(0.3f, 0.3f), 10 * Constants.Degree));
			UpdateState();
			initPoseRq.ResponsePort.Post(new DefaultUpdateResponseType());
		}

		[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public void InitPoseUnknownRequest(InitPoseUnknown initPoseUnknownRq)
		{
			DC.Contract.Requires(initPoseUnknownRq != null);
			DC.Contract.Requires(initPoseUnknownRq.Body != null);
			DC.Contract.Requires(initPoseUnknownRq.ResponsePort != null);

			_localizer.InitPoseUnknown();
			UpdateState();
            initPoseUnknownRq.ResponsePort.Post(new DefaultUpdateResponseType());
		}

		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public IEnumerator<ITask> OnSubscribe(Subscribe subscribeRq)
		{
			DC.Contract.Requires(subscribeRq != null);
			DC.Contract.Requires(subscribeRq.Body != null);
			DC.Contract.Requires(subscribeRq.ResponsePort != null);

			yield return SubscribeHelper(_subMgrPort, subscribeRq.Body, subscribeRq.ResponsePort).Choice(success => { }, LogError);
		}

        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public void OnDropDown(DsspDefaultDrop dropDownRq)
        {
            DC.Contract.Requires(dropDownRq != null);
            DC.Contract.Requires(dropDownRq.Body != null);

            _timerFacade.Dispose();
            DefaultDropHandler(dropDownRq);
        }

		void UpdateState()
		{
			//_state.FirstPoseCandidate = _localizer.GetPoseCandidates().First();
		    _state.FirstPoseCandidate = _localizer.CalculatePoseMean();
			SendNotification(_subMgrPort, new InitPose { Body = { Pose = _state.FirstPoseCandidate } });
		}

		bool IsPoseUnknown(Pose pose)
		{
			return double.IsNaN(pose.Bearing);
		}

		MatrixVizualizerServiceHelper _mv = new MatrixVizualizerServiceHelper();
		IEnumerator<ITask> Draw()
		{
			var h = new PoseHistogram(_localizer.Map, McLrfLocalizer.THETA_BIN_SIZE);
			h.Build(_localizer.Particles);
			var p = new DenseMatrix((int)h.Size.Y, (int)h.Size.X);
			var m = new DenseMatrix((int)h.Size.Y, (int)h.Size.X);
			var xyM = h.ToXyMarginal();
			for (var row = 0; row < (int)h.Size.Y; ++row)
				for (var col = 0; col < (int)h.Size.X; ++col)
				{
					p[(int)h.Size.Y - row - 1, col] = xyM[col, row];
					m[(int)h.Size.Y - row - 1, col] = _localizer.Map[col, row] ? 1 : 0;
				}

			yield return To.Exec(() => _mv.ShowMatrix(p));
			yield return To.Exec(() => _mv.ShowMatrix2(m));
		}

		static int i = 0;
		void Log()
		{
			var h = new PoseHistogram(_localizer.Map, McLrfLocalizer.THETA_BIN_SIZE);
			h.Build(_localizer.Particles);
			using (var f = File.CreateText("c:\\Temp\\qq" + i++ + ".txt"))
			{
				f.Write(h);
				f.Write(h.Bins.Sum(b => b.Samples.Count()));
			}
		}
    }
}