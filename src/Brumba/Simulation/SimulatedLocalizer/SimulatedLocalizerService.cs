using System.ComponentModel;
using Brumba.GenericLocalizer;
using Brumba.GenericFixedWheelVelocimeter;
using Brumba.Utils;
using Brumba.WaiterStupid;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Xna.Framework;

namespace Brumba.Simulation.SimulatedLocalizer
{
	[Contract(Contract.Identifier)]
    [DisplayName("Brumba Simulated Localizer")]
    [Description("no description provided")]
    class SimulatedLocalizerService : SimulatedEntityServiceBase
	{
	    [ServiceState]
        [InitialStatePartner(Optional = false)]
        SimulatedLocalizerState _state;

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = GenericLocalizer.Contract.Identifier)]
        GenericLocalizerOperations _genericLocalizerPort = new GenericLocalizerOperations();

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = GenericFixedWheelVelocimeter.Contract.Identifier)]
        GenericFixedWheelVelocimeterOperations _genericFixedWheelVelocimeterPort = new GenericFixedWheelVelocimeterOperations();

        [ServicePort("/SimulatedLocalizer", AllowMultipleInstances = true)]
        SimulatedLocalizerOperations _mainPort = new SimulatedLocalizerOperations();

        public SimulatedLocalizerService(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
		{
            (new Pose() as IFreezable).Freeze();
		}

        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public void OnGet(Get getRequest)
        {
            if (IsConnected)
                UpdateState();

            DefaultGetHandler(getRequest);
        }

	    [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericLocalizerPort")]
        public void OnGet(GenericLocalizer.Get get)
        {
            if (IsConnected)
                UpdateState();

            get.ResponsePort.Post(_state.Localizer);
        }

        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericFixedWheelVelocimeterPort")]
        public void OnGet(GenericFixedWheelVelocimeter.Get get)
        {
            if (IsConnected)
                UpdateState();

            get.ResponsePort.Post(_state.FixedWheelVelocimeter);
        }

        void UpdateState()
        {
            _state.Localizer.EstimatedPose = Entity.State.Pose.SimToMap();
            _state.FixedWheelVelocimeter.Velocity = ExtractVelocity();
        }

	    private Velocity ExtractVelocity()
	    {
	        var vLinear = Vector2.Dot(Entity.State.Pose.SimToMap().Direction(), Entity.State.Velocity.SimToMap());
	        var vAngular = Entity.State.AngularVelocity.SimToMapAngularVelocity();
            if (vLinear > _state.MaxVelocity.Linear)
                LogWarning(string.Format("Linear velocity ({0}) is greater than given maximum ({1})!", vLinear, _state.MaxVelocity.Linear));
            if (vAngular > _state.MaxVelocity.Angular)
                LogWarning(string.Format("Angular velocity ({0}) is greater than given maximum ({1})!", vAngular, _state.MaxVelocity.Angular));
	        return new Velocity(vLinear, vAngular);
	    }

	    protected override IConnectable GetState() { return _state; }
	}
}