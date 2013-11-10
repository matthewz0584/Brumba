using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.Services.SubscriptionManager;
using Microsoft.Robotics.Simulation.Physics;
using System.ComponentModel;
using Microsoft.Dss.Core.DsspHttp;
using sickPxy = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy;

namespace Brumba.Simulation.SimulatedLrf
{
	[DisplayName("Simulated Laser Range Finder")]
    [AlternateContract(sickPxy.Contract.Identifier)]
	[Contract(Contract.Identifier)]
	class SimulatedLrfService : SimulatedEntityServiceBase
	{
	    [ServiceState]
        private sickPxy.State _state = new sickPxy.State {Units = sickPxy.Units.Millimeters};

        [ServicePort("/SickLrf", AllowMultipleInstances = true)]
        private sickPxy.SickLRFOperations _mainPort = new sickPxy.SickLRFOperations();
		
		[Partner("SubMgr", Contract = Microsoft.Dss.Services.SubscriptionManager.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
		SubscriptionManagerPort _subMgrPort = new SubscriptionManagerPort();

	    Port<sickPxy.Replace> _internalReplacePort = new Port<sickPxy.Replace>();
        Port<RaycastResult> _raycastResultsPort = new Port<RaycastResult>();

		public SimulatedLrfService(DsspServiceCreationPort creationPort) :
			base(creationPort, Contract.Identifier)
		{
		}

        protected override Interleave ConcreteWaitingInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)),
                new ExclusiveReceiverGroup(),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                    Arbiter.Receive<sickPxy.Get>(true, _mainPort, GetHandler),
                    Arbiter.Receive<HttpGet>(true, _mainPort, GetHandler))
                );
        }

        protected override Interleave ConcreteActiveInterleave()
        {
            return new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)
                    ),
                new ExclusiveReceiverGroup(
                    Arbiter.Receive<sickPxy.Replace>(true, _mainPort, ReplaceHandler),
                    Arbiter.Receive<sickPxy.Replace>(true, _internalReplacePort, InternalReplaceHandler),
                    Arbiter.ReceiveWithIterator<sickPxy.Subscribe>(true, _mainPort, SubscribeHandler),
                    Arbiter.ReceiveWithIterator<sickPxy.ReliableSubscribe>(true, _mainPort, ReliableSubscribeHandler)
                    ),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                    Arbiter.Receive<sickPxy.Get>(true, _mainPort, GetHandler),
                    Arbiter.Receive<HttpGet>(true, _mainPort, GetHandler)
                    ));
        }

        protected override void OnInsertEntity()
        {
            _state.AngularRange = (int)Math.Abs(LrfEntity.RaycastProperties.EndAngle - LrfEntity.RaycastProperties.StartAngle);
            _state.AngularResolution = LrfEntity.RaycastProperties.AngleIncrement;

            try
            {
                LrfEntity.Register(_raycastResultsPort);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            Activate(Arbiter.Receive(true, _raycastResultsPort, RaycastResultsHandler));
        }

        // we just receive ray cast information from physics. Currently we just use
        // the distance measurement for each impact point reported. However, our simulation
        // engine also provides you the material properties so you can decide here to simulate
        // scattering, reflections, noise etc.
        void RaycastResultsHandler(RaycastResult result)
		{
		    var newState = new sickPxy.State
		        {
		            DistanceMeasurements =
		                Enumerable.Repeat((int) Math.Round(LrfEntity.RaycastProperties.Range*1000), result.SampleCount + 1).ToArray(),
		            AngularRange = _state.AngularRange,
		            AngularResolution = _state.AngularResolution,
		            Units = _state.Units,
		            LinkState = "Measurement received",
		            TimeStamp = DateTime.Now
		        };

            foreach (var pt in result.ImpactPoints)
				// the distance to the impact has been pre-calculted from the origin
				// and it's in the fourth element of the vector
				newState.DistanceMeasurements[pt.ReadingIndex] = (int)(pt.Position.W * 1000);

            // posting message to port in main interleave's exclusive section for synchronization purposes
            _internalReplacePort.Post(new sickPxy.Replace(newState));
		}

        public void GetHandler(sickPxy.Get get)
		{
			get.ResponsePort.Post(_state);
		}

        public void GetHandler(HttpGet get)
        {
            get.ResponsePort.Post(new HttpResponseType(_state));
        }

        public void ReplaceHandler(sickPxy.Replace replace)
        {
            LogError("SimulatedLrfService.Replace not implemented");
        }

        public void InternalReplaceHandler(sickPxy.Replace replace)
        {
            _state = replace.Body;
            _subMgrPort.Post(new Submit(_state, DsspActions.ReplaceRequest));
        }

        public IEnumerator<ITask> SubscribeHandler(sickPxy.Subscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
                    _subMgrPort.Post(new Submit(subscribe.Body.Subscriber, DsspActions.ReplaceRequest, _state, null)),
                LogError
                );
        }

        public IEnumerator<ITask> ReliableSubscribeHandler(sickPxy.ReliableSubscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
                    _subMgrPort.Post(new Submit(subscribe.Body.Subscriber, DsspActions.ReplaceRequest, _state, null)),
                LogError
                );
        }

        LaserRangeFinderExEntity LrfEntity
	    {
            get { return Entity as LaserRangeFinderExEntity; }
	    }
	}
}