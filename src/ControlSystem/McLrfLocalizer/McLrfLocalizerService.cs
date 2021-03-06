using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Brumba.Common;
using Brumba.GenericLocalizer;
using Brumba.MapProvider;
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

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = GenericLocalizer.Contract.Identifier)]
        private GenericLocalizerOperations _genericLocalizerPort = new GenericLocalizerOperations();

        [Partner("Odometry", Contract = OdometryPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        OdometryPxy.DiffDriveOdometryOperations _odometryProvider = new OdometryPxy.DiffDriveOdometryOperations();

        [Partner("Lrf", Contract = SickLrfPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SickLrfPxy.SickLRFOperations _lrf = new SickLrfPxy.SickLRFOperations();

		[Partner("Map", Contract = MapProviderPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		MapProviderPxy.MapProviderOperations _mapProvider = new MapProviderPxy.MapProviderOperations();

		[SubscriptionManagerPartner("SubMgr")]
		SubscriptionManagerPort _subMgrPort = new SubscriptionManagerPort();

        McLrfLocalizer _localizer;
        int _takeEachNthBeam;
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

	        var sparsifiedRp = _state.RangeFinderProperties.Sparsify(_state.BeamsNumber);
            _takeEachNthBeam = (int)Math.Round(sparsifiedRp.AngularResolution / _state.RangeFinderProperties.AngularResolution);
            _localizer = new McLrfLocalizer(map, sparsifiedRp, _state.ParticlesNumber);

			if (IsPoseUnknown(_state.EstimatedPose))
				_localizer.InitPoseUnknown();
			else
				_localizer.InitPose(_state.EstimatedPose, new Pose(new Vector2(0.3f, 0.3f), 10 * Constants.Degree));

		    yield return _odometryProvider.Get().Receive(os => _currentOdometry = (Pose) DssTypeHelper.TransformFromProxy(os.Pose));

			base.Start();

			MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
					Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateLocalizer))));

            yield return To.Exec(() => _timerFacade.Set());
        }

		IEnumerator<ITask> UpdateLocalizer(TimeSpan dt)
		{
			yield return Arbiter.JoinedReceive<SickLrfPxy.State, OdometryPxy.DiffDriveOdometryServiceState>(false,
                _lrf.Get(), _odometryProvider.Get(), 
                (lrfScan, odometry) =>
                    {
						DC.Contract.Requires(lrfScan != null);
						DC.Contract.Requires(lrfScan.DistanceMeasurements != null);
						DC.Contract.Requires(odometry != null);

                        var sw = Stopwatch.StartNew();

	                    var newOdometry = (Pose)DssTypeHelper.TransformFromProxy(odometry.Pose);
	                    _localizer.Update(newOdometry - _currentOdometry, PreprocessLrfScan(lrfScan));
						_currentOdometry = newOdometry;
	                    
						UpdateState();
						LogInfo("loc {0} for {1}", _state.EstimatedPose, sw.Elapsed.Milliseconds);
                    });
		}

	    [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public void OnQueryPose(QueryPose queryPoseRq)
		{
			DC.Contract.Requires(queryPoseRq != null);
			DC.Contract.Requires(queryPoseRq.Body != null);
			DC.Contract.Requires(queryPoseRq.ResponsePort != null);

			queryPoseRq.ResponsePort.Post(_state.EstimatedPose);
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

        [ServiceHandler(ServiceHandlerBehavior.Teardown, PortFieldName = "_genericLocalizerPort")]
        public void OnDropDownAlt(DsspDefaultDrop dropDownRq)
        {
            OnDropDown(dropDownRq);
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericLocalizerPort")]
        public void OnGet(GenericLocalizer.Get getRq)
        {
            DC.Contract.Requires(getRq != null);
            DC.Contract.Requires(getRq.Body != null);

            getRq.ResponsePort.Post(new GenericLocalizerState { EstimatedPose = _state.EstimatedPose });
        }

		void UpdateState()
		{
			//_state.EstimatedPose = _localizer.GetPoseCandidates().First();
		    _state.EstimatedPose = _localizer.CalculatePoseMean();
		    _state.Particles = _localizer.Particles.ToArray();

			SendNotification(_subMgrPort, new InitPose { Body = { Pose = _state.EstimatedPose } });
		}

		bool IsPoseUnknown(Pose pose)
		{
			return double.IsNaN(pose.Bearing);
		}

        IEnumerable<float> PreprocessLrfScan(SickLrfPxy.State lrfScan)
        {
            DC.Contract.Requires(lrfScan != null);
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<float>>().Count() == _state.BeamsNumber);

            return lrfScan.DistanceMeasurements.Where((d, i) => i % _takeEachNthBeam == 0).Select(d => d / 1000f).Take(_state.BeamsNumber);
        }
    }
}