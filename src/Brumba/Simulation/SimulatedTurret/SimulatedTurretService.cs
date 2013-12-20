using System.ComponentModel;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;

namespace Brumba.Simulation.SimulatedTurret
{
	[Contract(Contract.Identifier)]
    [DisplayName("Simulated turret")]
    [Description("no description provided")]
    class SimulatedTurretService : SimulatedEntityServiceBase
	{
		[ServiceState]
        readonly SimulatedTurretState _state = new SimulatedTurretState();
		
		[ServicePort("/SimulatedTurret", AllowMultipleInstances = true)]
        SimulatedTurretOperations _mainPort = new SimulatedTurretOperations();

        public SimulatedTurretService(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
		{
		}

        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public void OnGet(Get getRequest)
        {
			if (IsConnected)
                _state.BaseAngle = (Entity as TurretEntity).BaseAngle;

            DefaultGetHandler(getRequest);
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnSetBaseAngle(SetBaseAngle angleRequest)
        {
			if (FaultIfNotConnected(angleRequest))
				return;

            (Entity as TurretEntity).BaseAngle = angleRequest.Body.Angle;
            angleRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

		protected override ISimulationEntityServiceState GetState() { return _state; }
	}
}