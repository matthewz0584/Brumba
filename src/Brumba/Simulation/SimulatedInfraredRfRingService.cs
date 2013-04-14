using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brumba.Simulation.SimulatedTail;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation.SimulatedInfraredRfRing
{
	[Contract(Contract.Identifier)]
    [DisplayName("Simulated Infrared Rangefinder Ring")]
    [Description("no description provided")]
	class SimulatedInfraredRfRingService : DsspServiceBase
	{
		[ServiceState]
		SimulatedInfraredRfRingState _state = new SimulatedInfraredRfRingState();
		
		[ServicePort("/SimulatedInfraredRfRing", AllowMultipleInstances = true)]
		SimulatedInfraredRfRingOperations _mainPort = new SimulatedInfraredRfRingOperations();

        SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        InfraredRfRingEntity _rfRing;

        public SimulatedInfraredRfRingService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}

		protected override void Start()
		{
            SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _simEngineNotifyPort);

            base.Start();

            //Killing default main port interleave. From now on I control main port interleave
            MainPortInterleave.CombineWith(new Interleave(
                    new TeardownReceiverGroup(Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)),
                    new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup()));
		}

        void OnInsertEntity(InsertSimulationEntity entity)
        {
            LogInfo("SimulatedInfraredRfRing OnInsertEntity called");

            _rfRing = entity.Body as InfraredRfRingEntity;
            _rfRing.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedInfraredRfRing OnDeleteEntity called");

            _rfRing = null;
            _state.Connected = false;
            _state.Distances = new List<float>();

            SetUpForWaitingForEntity();
        }

        void OnGet(Get getRequest)
        {
            if (_rfRing != null)
                _state.Distances = _rfRing.GetDistances().ToList();

            DefaultGetHandler(getRequest);
        }

        void SetUpForWaitingForEntity()
        {
            ResetMainPortInterleave(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                        Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)
                        ),
                    new ExclusiveReceiverGroup(),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                        Arbiter.Receive<Get>(true, _mainPort, OnGet)
                        )));
        }

        void SetUpForControlOfEntity()
        {
            ResetMainPortInterleave(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
                        Arbiter.Receive<DeleteSimulationEntity>(false, _simEngineNotifyPort, OnDeleteEntity)
                        ),
                    new ExclusiveReceiverGroup(),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                        Arbiter.Receive<Get>(true, _mainPort, OnGet)
                        )));
        }

        void ResetMainPortInterleave(Interleave ileave)
        {
            Activate(ileave);
            MainPortInterleave = ileave;
        }
	}
}