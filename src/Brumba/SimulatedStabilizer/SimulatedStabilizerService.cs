using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation.SimulatedStabilizer
{
	[Contract(Contract.Identifier)]
    [DisplayName("SimulatedStabilizer")]
    [Description("SimulatedStabilizer service (no description provided)")]
	class SimulatedStabilizerService : DsspServiceBase
	{
		[ServiceState]
		private SimulatedStabilizerState _state = new SimulatedStabilizerState();
		
		[ServicePort("/SimulatedAckermanFourWheels", AllowMultipleInstances = true)]
		private SimulatedStabilizerOperations _mainPort = new SimulatedStabilizerOperations();

        private SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        //private AckermanFourWheelsEntity _vehicle;

        public SimulatedStabilizerService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}

		protected override void Start()
		{
            SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _simEngineNotifyPort);

            base.Start();

            //Killing default main port interleave. From now on I control main port interleave
            //MainPortInterleave.CombineWith(new Interleave(
            //        new TeardownReceiverGroup(Arbiter.Receive<Break>(false, _mainPort, b => { })),
            //        new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup()));
            //_mainPort.Post(new Break());

            //SetUpForWaitingForEntity(); 
		}

        //private void OnInsertEntity(InsertSimulationEntity entity)
        //{
        //    LogInfo("SimulatedAckermanFourWheels OnInsertEntity called");
            
        //    _vehicle = entity.Body as AckermanFourWheelsEntity;
        //    _vehicle.ServiceContract = Contract.Identifier;
        //    _state.Connected = true;

        //    SetUpForControlOfEntity();
        //}

        //private void OnDeleteEntity(DeleteSimulationEntity entity)
        //{
        //    LogInfo("SimulatedAckermanFourWheels OnDeleteEntity called");
            
        //    _vehicle = null;
        //    _state.Connected = false;
        //    _state.MotorPower = 0;
        //    _state.SteerAngle = 0;

        //    SetUpForWaitingForEntity();
        //}

        //private void SetUpForWaitingForEntity()
        //{
        //    ResetMainPortInterleave(new Interleave(
        //            new TeardownReceiverGroup(
        //                Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
        //                Arbiter.Receive<InsertSimulationEntity>(false, _simEngineNotifyPort, OnInsertEntity)
        //                ),
        //            new ExclusiveReceiverGroup(),
        //            new ConcurrentReceiverGroup(
        //                Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler)
        //                )));
        //}

        //private void SetUpForControlOfEntity()
        //{
        //    ResetMainPortInterleave(new Interleave(
        //            new TeardownReceiverGroup(
        //                Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler),
        //                Arbiter.Receive<DeleteSimulationEntity>(false, _simEngineNotifyPort, OnDeleteEntity)
        //                ),
        //            new ExclusiveReceiverGroup(
        //                Arbiter.Receive<SetMotorPower>(true, _mainPort, OnSetMotorPower),
        //                Arbiter.Receive<SetSteerAngle>(true, _mainPort, OnSetSteerAngle),
        //                Arbiter.Receive<Break>(true, _mainPort, OnBreak)
        //                ),
        //            new ConcurrentReceiverGroup(
        //                Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler)
        //                )));
        //}

        //private void ResetMainPortInterleave(Interleave ileave)
        //{
        //    Activate(ileave);
        //    MainPortInterleave = ileave;
        //}
	}
}