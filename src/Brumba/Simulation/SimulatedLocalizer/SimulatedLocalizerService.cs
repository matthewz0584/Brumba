using System.ComponentModel;
using Brumba.GenericLocalizer;
using Brumba.GenericVelocimeter;
using Brumba.Utils;
using Brumba.WaiterStupid;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;

namespace Brumba.Simulation.SimulatedLocalizer
{
	[Contract(Contract.Identifier)]
    [DisplayName("Brumba Simulated Localizer")]
    [Description("no description provided")]
    class SimulatedLocalizerService : SimulatedEntityServiceBase
	{
		[ServiceState]
        readonly SimulatedLocalizerState _state = new SimulatedLocalizerState();

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = GenericLocalizer.Contract.Identifier)]
        private GenericLocalizerOperations _genericLocalizerPort = new GenericLocalizerOperations();

        [AlternateServicePort(AllowMultipleInstances = true, AlternateContract = GenericVelocimeter.Contract.Identifier)]
        private GenericVelocimeterOperations _genericVelocimeterPort = new GenericVelocimeterOperations();

        [ServicePort("/SimulatedLocalizer", AllowMultipleInstances = true)]
        private SimulatedLocalizerOperations _mainPort = new SimulatedLocalizerOperations();

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

            DefaultGetHandler(get);
        }

        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericVelocimeterPort")]
        public void OnGet(GenericVelocimeter.Get get)
        {
            if (IsConnected)
                UpdateState();

            DefaultGetHandler(get);
        }

        void UpdateState()
        {
            _state.Localizer.EstimatedPose = Entity.State.Pose.SimToMap();
            _state.Velocimeter.Velocity = new Pose(Entity.State.Velocity.SimToMap(), Entity.State.Velocity.SimToMapAngularVelocity());
        }

		protected override IConnectable GetState() { return _state; }
	}
}