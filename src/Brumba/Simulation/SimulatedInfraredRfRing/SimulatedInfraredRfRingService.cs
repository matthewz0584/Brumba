using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;

namespace Brumba.Simulation.SimulatedInfraredRfRing
{
	[Contract(Contract.Identifier)]
    [DisplayName("Simulated Infrared Rangefinder Ring")]
    [Description("no description provided")]
    class SimulatedInfraredRfRingService : SimulatedEntityServiceBase
	{
		[ServiceState]
		SimulatedInfraredRfRingState _state = new SimulatedInfraredRfRingState();
		
		[ServicePort("/SimulatedInfraredRfRing", AllowMultipleInstances = true)]
		SimulatedInfraredRfRingOperations _mainPort = new SimulatedInfraredRfRingOperations();

        public SimulatedInfraredRfRingService(DsspServiceCreationPort creationPort)
			: base(creationPort, Contract.Identifier)
		{
		}

	    protected override Interleave ConcreteWaitingInterleave()
	    {
	        return new Interleave(
	            new TeardownReceiverGroup(
	                Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)
	                ),
	            new ExclusiveReceiverGroup(),
	            new ConcurrentReceiverGroup(
	                Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
	                Arbiter.Receive<Get>(true, _mainPort, OnGet)
	                ));
	    }

	    protected override Interleave ConcreteActiveInterleave()
	    {
	        return new Interleave(
	            new TeardownReceiverGroup(
	                Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)
	                ),
	            new ExclusiveReceiverGroup(),
	            new ConcurrentReceiverGroup(
	                Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
	                Arbiter.Receive<Get>(true, _mainPort, OnGet)
	                ));
	    }

	    protected override void OnInsertEntity()
        {
            LogInfo("SimulatedInfraredRfRing OnInsertEntity called");
            _state.Connected = true;
        }

        protected override void OnDeleteEntity()
        {
            LogInfo("SimulatedInfraredRfRing OnDeleteEntity called");
            _state.Connected = false;
            _state.Distances = new List<float>();
        }

        void OnGet(Get getRequest)
        {
            if (Entity != null)
                _state.Distances = (Entity as InfraredRfRingEntity).GetDistances().ToList();

            DefaultGetHandler(getRequest);
        }
	}
}