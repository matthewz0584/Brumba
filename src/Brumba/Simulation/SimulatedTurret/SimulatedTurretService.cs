using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation.SimulatedTurret
{
	[Contract(Contract.Identifier)]
    [DisplayName("Simulated turret")]
    [Description("no description provided")]
	class SimulatedTurretService : DsspServiceBase
	{
		[ServiceState]
        SimulatedTurretState _state = new SimulatedTurretState();
		
		[ServicePort("/SimulatedTurret", AllowMultipleInstances = true)]
        SimulatedTurretOperations _mainPort = new SimulatedTurretOperations();

        SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        TurretEntity _turret;

        public SimulatedTurretService(DsspServiceCreationPort creationPort)
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
            LogInfo("SimulatedTurret OnInsertEntity called");

            _turret = entity.Body as TurretEntity;
            _turret.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedTurret OnDeleteEntity called");

            _turret = null;
            _state.Connected = false;

            SetUpForWaitingForEntity();
        }

        void OnGet(Get getRequest)
        {
            if (_turret != null)
                _state.BaseAngle = _turret.BaseAngle;

            DefaultGetHandler(getRequest);
        }

        void OnSetBaseAngle(SetBaseAngle angleRequest)
        {
            _turret.BaseAngle = angleRequest.Body.Angle;
            angleRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
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
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<SetBaseAngle>(true, _mainPort, OnSetBaseAngle)
                        ),
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