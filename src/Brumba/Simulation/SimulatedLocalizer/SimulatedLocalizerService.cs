using System.ComponentModel;
using Brumba.DwaNavigator;
using Brumba.GenericLocalizer;
using Brumba.GenericVelocimeter;
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

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = GenericVelocimeter.Contract.Identifier)]
        GenericVelocimeterOperations _genericVelocimeterPort = new GenericVelocimeterOperations();

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

        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericVelocimeterPort")]
        public void OnGet(GenericVelocimeter.Get get)
        {
            if (IsConnected)
                UpdateState();

            get.ResponsePort.Post(_state.Velocimeter);
        }

        void UpdateState()
        {
            _state.Localizer.EstimatedPose = Entity.State.Pose.SimToMap();
            _state.Velocimeter.Velocity = ExtractVelocity();
        }

	    private Pose ExtractVelocity()
	    {
	        var vLinear = Entity.State.Velocity.SimToMap();
	        var vAngular = Entity.State.AngularVelocity.SimToMapAngularVelocity();
            if (vLinear.Length() > _state.MaxVelocity.Linear)
                LogWarning(string.Format("Linear velocity ({0}) is greater than given maximum ({1})!", vLinear.Length(), _state.MaxVelocity.Linear));
            if (vAngular > _state.MaxVelocity.Angular)
                LogWarning(string.Format("Angular velocity ({0}) is greater than given maximum ({1})!", vAngular, _state.MaxVelocity.Angular));
	        return new Pose(
	            vLinear.Length() <= _state.MaxVelocity.Linear ? vLinear : Vector2.Normalize(vLinear) * (float)_state.MaxVelocity.Linear,
	            vAngular <= _state.MaxVelocity.Angular ? vAngular : _state.MaxVelocity.Angular);
	    }

	    protected override IConnectable GetState() { return _state; }
	}
}