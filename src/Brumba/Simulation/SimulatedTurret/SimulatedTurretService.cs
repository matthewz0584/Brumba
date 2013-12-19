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
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<SetBaseAngle>(true, _mainPort, OnSetBaseAngle)
                        ),
                    new ConcurrentReceiverGroup(
                        Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
                        Arbiter.Receive<Get>(true, _mainPort, OnGet)
                        ));
	    }

        void OnGet(Get getRequest)
        {
	        _state.Connected = Connected;

            if (Entity != null)
                _state.BaseAngle = (Entity as TurretEntity).BaseAngle;

            DefaultGetHandler(getRequest);
        }

        void OnSetBaseAngle(SetBaseAngle angleRequest)
        {
            (Entity as TurretEntity).BaseAngle = angleRequest.Body.Angle;
            angleRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }
	}
}