using System.ComponentModel;
using Microsoft.Ccr.Core;
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
        SimulatedTurretState _state = new SimulatedTurretState();
		
		[ServicePort("/SimulatedTurret", AllowMultipleInstances = true)]
        SimulatedTurretOperations _mainPort = new SimulatedTurretOperations();

        public SimulatedTurretService(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
		{
		}

        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public void OnGet(Get getRequest)
        {
			if (Connected)
                _state.BaseAngle = (Entity as TurretEntity).BaseAngle;

			_state.Connected = Connected;
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
	}
}