﻿using System;
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
	[DisplayName("Simulated Laser Range Finder")]
    [AlternateContract(SickLrfPxy.Contract.Identifier)]
	[Contract(Contract.Identifier)]
	class SimulatedLrfService : SimulatedEntityServiceBase
	{
		[ServiceState]
		private SimulatedLrfState _state = new SimulatedLrfState { SickLrfState = new SickLrf.State { Units = SickLrf.Units.Millimeters } };

        [AlternateServicePort("/SickLrf", AllowMultipleInstances = true, AlternateContract = SickLrfPxy.Contract.Identifier)]
        private SickLrfPxy.SickLRFOperations _sickLrfPort = new SickLrfPxy.SickLRFOperations();

		[ServicePort(AllowMultipleInstances = true)]
		private SimulatedLrfOperations _mainPort = new SimulatedLrfOperations();
		
		[Partner("SubMgr", Contract = Microsoft.Dss.Services.SubscriptionManager.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
		SubscriptionManagerPort _subMgrPort = new SubscriptionManagerPort();

	    Port<SickLrf.State> _internalReplacePort = new Port<SickLrf.State>();
        Port<RaycastResult> _raycastResultsPort = new Port<RaycastResult>();

		public SimulatedLrfService(DsspServiceCreationPort creationPort) :
			base(creationPort, Contract.Identifier)
		{
		}

        protected override Interleave ConcreteWaitingInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _sickLrfPort, DefaultDropHandler),
					
					Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)),
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _sickLrfPort, DefaultLookupHandler),
                    Arbiter.Receive<SickLrfPxy.Get>(true, _sickLrfPort, GetHandler),
                    Arbiter.Receive<HttpGet>(true, _sickLrfPort, GetHandler),
					
					Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
					Arbiter.Receive<Get>(true, _mainPort, GetHandler)
					)
                );
        }

        protected override Interleave ConcreteActiveInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _sickLrfPort, DefaultDropHandler),
					
					Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)
                    ),
                new ExclusiveReceiverGroup(
                    Arbiter.Receive<SickLrfPxy.Replace>(true, _sickLrfPort, ReplaceHandler),
                    Arbiter.ReceiveWithIterator<SickLrfPxy.Subscribe>(true, _sickLrfPort, SubscribeHandler),
                    Arbiter.ReceiveWithIterator<SickLrfPxy.ReliableSubscribe>(true, _sickLrfPort, ReliableSubscribeHandler),

					Arbiter.Receive(true, _internalReplacePort, InternalReplaceHandler)
                    ),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _sickLrfPort, DefaultLookupHandler),
                    Arbiter.Receive<SickLrfPxy.Get>(true, _sickLrfPort, GetHandler),
                    Arbiter.Receive<HttpGet>(true, _sickLrfPort, GetHandler),
                    
					Arbiter.Receive(true, _raycastResultsPort, RaycastResultsHandler),

					Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
					Arbiter.Receive<Get>(true, _mainPort, GetHandler)
                    ));
        }

        protected override void OnInsertEntity()
        {
            _state.SickLrfState.AngularRange = (int)Math.Abs(LrfEntity.RaycastProperties.EndAngle - LrfEntity.RaycastProperties.StartAngle);
			_state.SickLrfState.AngularResolution = LrfEntity.RaycastProperties.AngleIncrement;

            try
            {
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
            if (LrfEntity == null)
                LogError("Lrf is null!!!");
            if (_state == null)
                LogError("_state is null!!!");
			var newState = new SickLrf.State
		        {
		            DistanceMeasurements =
		                Enumerable.Repeat((int) Math.Round(LrfEntity.RaycastProperties.Range*1000), result.SampleCount + 1).ToArray(),
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

        public void GetHandler(SickLrfPxy.Get get)
        {
	        get.ResponsePort.Post(DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State);
        }

		public void GetHandler(HttpGet get)
        {
			get.ResponsePort.Post(new HttpResponseType(DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State));
        }

		public void GetHandler(Get get)
		{
			_state.Connected = Connected;
			get.ResponsePort.Post(_state);
		}

        public void ReplaceHandler(SickLrfPxy.Replace replace)
        {
            LogError("SimulatedLrfService.Replace not implemented");
        }

		public void InternalReplaceHandler(SickLrf.State sickLrfState)
        {
			_state.SickLrfState = sickLrfState;
			_subMgrPort.Post(new Submit(DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State, DsspActions.ReplaceRequest));
        }

        public IEnumerator<ITask> SubscribeHandler(SickLrfPxy.Subscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
					_subMgrPort.Post(new Submit(subscribe.Body.Subscriber, DsspActions.ReplaceRequest, DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State, null)),
                LogError
                );
        }

        public IEnumerator<ITask> ReliableSubscribeHandler(SickLrfPxy.ReliableSubscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
                    _subMgrPort.Post(new Submit(subscribe.Body.Subscriber, DsspActions.ReplaceRequest, DssTypeHelper.TransformToProxy(_state.SickLrfState) as SickLrfPxy.State, null)),
                LogError
                );
        }

        LaserRangeFinderExEntity LrfEntity
	    {
            get { return Entity as LaserRangeFinderExEntity; }
	    }
	}
}