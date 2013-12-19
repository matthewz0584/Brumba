using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation
{
	abstract class SimulatedEntityServiceBase : DsspServiceBase
	{
	    readonly SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
	    readonly string _contract;

		public VisualEntity Entity { get; set; }

		public bool Connected { get; set; }

		protected SimulatedEntityServiceBase(DsspServiceCreationPort creationPort, string contract)
			: base(creationPort)
	    {
	        _contract = contract;
	    }

	    protected override void Start()
		{
            SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _simEngineNotifyPort);

            base.Start();

            //Killing default main port interleave. From now on I control main port interleave
            var shutdownInterleave = new Port<bool>();
            MainPortInterleave.CombineWith(new Interleave(
                    new TeardownReceiverGroup(Arbiter.Receive(false, shutdownInterleave, shutdown => { })),
                    new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup()));
            shutdownInterleave.Post(true);

            SetUpForWaitingForEntity();
		}

        protected abstract Interleave ConcreteWaitingInterleave();
        protected abstract Interleave ConcreteActiveInterleave();
        protected virtual void OnInsertEntity() {}
        protected virtual void OnDeleteEntity() {}

        void OnInsertEntity(InsertSimulationEntity entity)
        {
            Entity = entity.Body;
            Entity.ServiceContract = _contract;

            OnInsertEntity();

            SetUpForControlOfEntity();

			LogInfo(string.Format("{0} entity inserted", entity.Body));
	        Connected = true;
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
	        Connected = false;
			LogInfo(string.Format("{0} entity deleted", entity.Body));

            Entity = null;
            
            OnDeleteEntity();

            SetUpForWaitingForEntity();
        }

        void SetUpForWaitingForEntity()
        {
            ResetMainPortInterleave(ConcreteWaitingInterleave());
            MainPortInterleave.CombineWith(new Interleave(
                                               new TeardownReceiverGroup(
                                                   Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)
                                                   ),
                                               new ExclusiveReceiverGroup(),
                                               new ConcurrentReceiverGroup()));
        }

	    void SetUpForControlOfEntity()
        {
            ResetMainPortInterleave(ConcreteActiveInterleave());
            MainPortInterleave.CombineWith(new Interleave(
                    new TeardownReceiverGroup(
                        Arbiter.Receive<DeleteSimulationEntity>(false, _simEngineNotifyPort, OnDeleteEntity)
                        ),
                    new ExclusiveReceiverGroup(),
                    new ConcurrentReceiverGroup()));
        }

	    void ResetMainPortInterleave(Interleave ileave)
        {
            Activate(ileave);
            MainPortInterleave = ileave;
        }
	}
}