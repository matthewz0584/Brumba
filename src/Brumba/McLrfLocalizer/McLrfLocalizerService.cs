using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Brumba.DiffDriveOdometry;
using Brumba.DsspUtils;
using Brumba.MapProvider;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
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
        [ServiceState]
		[InitialStatePartner(Optional = false)]
        McLrfLocalizerState _state;

        [ServicePort("/McLrfLocalizer", AllowMultipleInstances = true)]
        McLrfLocalizerOperations _mainPort = new McLrfLocalizerOperations();

        [Partner("Odometry", Contract = OdometryPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        OdometryPxy.DiffDriveOdometryOperations _odometryProvider = new OdometryPxy.DiffDriveOdometryOperations();

        [Partner("Lrf", Contract = SickLrfPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        SickLrfPxy.SickLRFOperations _lrf = new SickLrfPxy.SickLRFOperations();

		[Partner("Map", Contract = MapProviderPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		MapProviderPxy.MapProviderOperations _mapProvider = new MapProviderPxy.MapProviderOperations();

        McLrfLocalizer _localizer;
        TimerFacade _timerFacade;
        Pose _currentOdometry;

	    public McLrfLocalizerService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        protected override void Start()
        {
			_timerFacade = new TimerFacade(this, _state.DeltaT);

	        base.Start();

            SpawnIterator(StartIt);
        }

	    IEnumerator<ITask> StartIt()
	    {
			OccupancyGrid map = null;
		    yield return _mapProvider.Get().Receive(ms => map = (OccupancyGrid) DssTypeHelper.TransformFromProxy(ms.Map));
			map.Freeze();

			_localizer = new McLrfLocalizer(map, _state.RangeFinderProperties, _state.ParticlesNumber);

			if (IsPoseUnknown(_state.FirstPoseCandidate))
				_localizer.InitPoseUnknown();
			else
				_localizer.InitPose(_state.FirstPoseCandidate, new Pose(new Vector2(0.3f, 0.3f), 10 * Constants.Degree));

		    yield return _odometryProvider.Get().Receive(os => _currentOdometry = (Pose) DssTypeHelper.TransformFromProxy(os.State.Pose));

            MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
                    Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateLocalizer))));

            yield return To.Exec(() => _timerFacade.Set());
        }

		IEnumerator<ITask> UpdateLocalizer(TimeSpan dt)
        {
            yield return Arbiter.JoinedReceive(false, _lrf.Get().P0, _odometryProvider.Get().P0, 
                (lrfScan, odometry) =>
                    {
                        _localizer.Update(((DiffDriveOdometryServiceState)DssTypeHelper.TransformFromProxy(odometry)).State.Pose - _currentOdometry,
                                            lrfScan.DistanceMeasurements.Select(d => d / 1000f));
	                    _state.FirstPoseCandidate = _localizer.GetPoseCandidates().First();
						
	                    Log();
                    });
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

        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public void OnDropDown(DsspDefaultDrop dropDownRq)
        {
            DC.Contract.Requires(dropDownRq != null);
            DC.Contract.Requires(dropDownRq.Body != null);

            _timerFacade.Dispose();
            DefaultDropHandler(dropDownRq);
        }

		bool IsPoseUnknown(Pose pose)
		{
			return double.IsNaN(pose.Bearing);
		}
    }
}