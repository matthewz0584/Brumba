using System.ComponentModel;
using Brumba.GenericLocalizer;
using Brumba.Utils;
using Brumba.WaiterStupid;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;

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

        [ServicePort("/SimulatedLocalizer", AllowMultipleInstances = true)]
        private SimulatedLocalizerOperations _mainPort = new SimulatedLocalizerOperations();

        public SimulatedLocalizerService(DsspServiceCreationPort creationPort)
            : base(creationPort, Contract.Identifier)
		{
            ((IFreezable) new Pose()).Freeze();
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

        void UpdateState()
        {
            _state.EstimatedPose = new Pose(
                new Vector2(Entity.State.Pose.Position.X, Entity.State.Pose.Position.Z),
                UIMath.QuaternionToEuler(Entity.State.Pose.Orientation).Y);
        }

		protected override IConnectable GetState() { return _state; }
	}
}