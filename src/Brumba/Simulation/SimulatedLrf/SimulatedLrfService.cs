using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Services.SubscriptionManager;
using Microsoft.Robotics.Simulation.Physics;
using System.ComponentModel;
using Microsoft.Dss.Core.DsspHttp;
using SickLrf = Microsoft.Robotics.Services.Sensors.SickLRF;
using SickLrfPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;

namespace Brumba.Simulation.SimulatedLrf
{
	[DisplayName("Brumba Simulated Laser Range Finder")]
    [AlternateContract(SickLrfPxy.Contract.Identifier)]
	[Contract(Contract.Identifier)]
	class SimulatedLrfService : SimulatedEntityServiceBase
	{
		[ServiceState]
		readonly SimulatedLrfState _state = new SimulatedLrfState { SickLrfState = new SickLrf.State { Units = SickLrf.Units.Millimeters } };

        [AlternateServicePort("/SickLrf", AllowMultipleInstances = true, AlternateContract = SickLrfPxy.Contract.Identifier)]
        SickLrfPxy.SickLRFOperations _sickLrfPort = new SickLrfPxy.SickLRFOperations();

		[ServicePort(AllowMultipleInstances = true)]
		SimulatedLrfOperations _mainPort = new SimulatedLrfOperations();
		
		[Partner("SubMgr", Contract = Microsoft.Dss.Services.SubscriptionManager.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
		SubscriptionManagerPort _subMgrPort = new SubscriptionManagerPort();

	    Port<SickLrf.State> _internalReplacePort = new Port<SickLrf.State>();
        Port<RaycastResult> _raycastResultsPort = new Port<RaycastResult>();

		public SimulatedLrfService(DsspServiceCreationPort creationPort) :
			base(creationPort, Contract.Identifier)
		{
		}

		protected override void Start()
		{
			base.Start();

			MainPortInterleave.CombineWith(
				new Interleave(new ExclusiveReceiverGroup(Arbiter.Receive(true, _internalReplacePort, InternalReplaceHandler)),
				   new ConcurrentReceiverGroup()));
		}

        protected override void OnInsertEntity()
        {
            _state.SickLrfState.AngularRange = (int)Math.Abs(LrfEntity.RaycastProperties.EndAngle - LrfEntity.RaycastProperties.StartAngle);
			_state.SickLrfState.AngularResolution = LrfEntity.RaycastProperties.AngleIncrement;
	        _state.MaxRange = LrfEntity.RaycastProperties.Range;

            try
            {
				MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(),
					new ConcurrentReceiverGroup(Arbiter.Receive(true, _raycastResultsPort, RaycastResultsHandler))));

                LrfEntity.Register(_raycastResultsPort);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        // we just receive ray cast information from physics. Currently we just use
        // the distance measurement for each impact point reported. However, our simulation
        // engine also provides you the material properties so you can decide here to simulate
        // scattering, reflections, noise etc.
        void RaycastResultsHandler(RaycastResult result)
		{
			var newState = new SickLrf.State
		        {
		            DistanceMeasurements =
						Enumerable.Repeat((int)Math.Round(_state.MaxRange * 1000), result.SampleCount + 1).ToArray(),
		            AngularRange = _state.SickLrfState.AngularRange,
					AngularResolution = _state.SickLrfState.AngularResolution,
					Units = _state.SickLrfState.Units,
		            LinkState = "Measurement received",
		            TimeStamp = DateTime.Now
		        };

            foreach (var pt in result.ImpactPoints)
				// the distance to the impact has been pre-calculted from the origin
				// and it's in the fourth element of the vector
				newState.DistanceMeasurements[pt.ReadingIndex] = (int)(pt.Position.W * 1000);

            // posting message to port in main interleave's exclusive section for synchronization purposes
            _internalReplacePort.Post(newState);
		}

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_sickLrfPort")]
        public void GetHandler(SickLrfPxy.Get get)
        {
	        get.ResponsePort.Post(DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State);
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_sickLrfPort")]
		public void GetHandler(HttpGet get)
        {
			get.ResponsePort.Post(new HttpResponseType(DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State));
        }

		//[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		//public void GetHandler(Get get)
		//{
		//	get.ResponsePort.Post(_state);
		//}

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_sickLrfPort")]
		public void ReplaceHandler(SickLrfPxy.Replace replace)
        {
            LogError("SimulatedLrfService.Replace not implemented");
        }

		void InternalReplaceHandler(SickLrf.State sickLrfState)
        {
			_state.SickLrfState = sickLrfState;
			_subMgrPort.Post(new Submit(DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State, DsspActions.ReplaceRequest));
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_sickLrfPort")]
		public IEnumerator<ITask> SubscribeHandler(SickLrfPxy.Subscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
					_subMgrPort.Post(new Submit(subscribe.Body.Subscriber, DsspActions.ReplaceRequest, DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State, null)),
                LogError
                );
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_sickLrfPort")]
		public IEnumerator<ITask> ReliableSubscribeHandler(SickLrfPxy.ReliableSubscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
                    _subMgrPort.Post(new Submit(subscribe.Body.Subscriber, DsspActions.ReplaceRequest, DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State, null)),
                LogError
                );
        }

		protected override ISimulationEntityServiceState GetState() { return _state; }

        LaserRangeFinderExEntity LrfEntity
	    {
            get { return Entity as LaserRangeFinderExEntity; }
	    }
	}
}