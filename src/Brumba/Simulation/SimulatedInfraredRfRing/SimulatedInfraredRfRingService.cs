using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
		readonly SimulatedInfraredRfRingState _state = new SimulatedInfraredRfRingState();
		
		[ServicePort("/SimulatedInfraredRfRing", AllowMultipleInstances = true)]
		SimulatedInfraredRfRingOperations _mainPort = new SimulatedInfraredRfRingOperations();

        public SimulatedInfraredRfRingService(DsspServiceCreationPort creationPort)
			: base(creationPort, Contract.Identifier)
		{
		}

        protected override void OnDeleteEntity()
        {
            _state.Distances = new List<float>();
        }

        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public void OnGet(Get getRequest)
        {
            if (IsConnected)
                _state.Distances = (Entity as InfraredRfRingEntity).GetDistances().ToList();

            DefaultGetHandler(getRequest);
        }

		protected override ISimulationEntityServiceState GetState() { return _state; }
	}
}