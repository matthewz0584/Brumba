using System;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using W3C.Soap;

namespace Brumba.Simulation
{
	interface ISimulationEntityServiceState
	{
		bool IsConnected { get; set; }
	}

	abstract class SimulatedEntityServiceBase : DsspServiceBase
	{
	    readonly SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
	    readonly string _contract;

		public VisualEntity Entity { get; set; }

		public bool IsConnected { get { return GetState().IsConnected; } }

		protected SimulatedEntityServiceBase(DsspServiceCreationPort creationPort, string contract)
			: base(creationPort)
		{
			_contract = contract;
		}

		protected override void Start()
		{
            SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _simEngineNotifyPort);

            base.Start();

			MainPortInterleave.CombineWith(new Interleave(
				new ExclusiveReceiverGroup(Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)),
				new ConcurrentReceiverGroup()));
		}

        protected virtual void OnInsertEntity() {}
        protected virtual void OnDeleteEntity() {}

		protected abstract ISimulationEntityServiceState GetState();

        void OnInsertEntity(InsertSimulationEntity entity)
        {
            Entity = entity.Body;
            Entity.ServiceContract = _contract;

            OnInsertEntity();

			LogInfo(string.Format("{0} entity inserted", entity.Body));
			GetState().IsConnected = true;

	        MainPortInterleave.CombineWith(new Interleave(
				new ExclusiveReceiverGroup(Arbiter.Receive<DeleteSimulationEntity>(false, _simEngineNotifyPort, OnDeleteEntity)),
				new ConcurrentReceiverGroup()));
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
			GetState().IsConnected = false;
			LogInfo(string.Format("{0} entity deleted", entity.Body));

            Entity = null;
            
            OnDeleteEntity();

			MainPortInterleave.CombineWith(new Interleave(
				new ExclusiveReceiverGroup(Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)),
				new ConcurrentReceiverGroup()));
        }

		public bool FaultIfNotConnected<TBody, TResponseSuccess>(DsspOperation<TBody, PortSet<TResponseSuccess, Fault>> message)
			where TBody : new()
		{
			return FaultIfNotConnected(message.ResponsePort, message.GetType());
		}

		public bool FaultIfNotConnected<TBody, TResponseSuccess>(DsspOperation<TBody, DsspResponsePort<TResponseSuccess>> message)
			where TBody : new()
		{
			return FaultIfNotConnected(message.ResponsePort, message.GetType());
		}

		bool FaultIfNotConnected<TResponseSuccess>(PortSet<TResponseSuccess, Fault> responsePort, Type messageType)
		{
			if (IsConnected)
				return false;

			LogError(string.Format("Call of simulation service handler for {0} operation while entity is not connected.", messageType));
		    responsePort.Post(new Fault
		        {
                    Code = new FaultCode(),
		            Reason = new[] {new ReasonText {Lang = "en-EN", Value = "Simulation entity is not connected."}}
		        });
			return true;			
		}
	}
}