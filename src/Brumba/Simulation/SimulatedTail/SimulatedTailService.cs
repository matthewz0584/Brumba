using System.ComponentModel;
using System.Linq;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Xna.Framework;

namespace Brumba.Simulation.SimulatedTail
{
	[Contract(Contract.Identifier)]
    [DisplayName("Simulated tail")]
    [Description("no description provided")]
    class SimulatedTailService : SimulatedEntityServiceBase
	{
		[ServiceState]
		readonly SimulatedTailState _state = new SimulatedTailState();
		
		[ServicePort("/SimulatedTail", AllowMultipleInstances = true)]
		SimulatedTailOperations _mainPort = new SimulatedTailOperations();

        public SimulatedTailService(DsspServiceCreationPort creationPort)
			: base(creationPort, Contract.Identifier)
		{
		}

		[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public void OnGet(Get getRequest)
        {
            if (IsConnected)
            {
                _state.WheelToGroundDistances = TailEntity.GroundRangefinders.Select(grf => grf.Distance).ToList();
                _state.Segment1Angle = TailEntity.Segment1Angle;
                _state.Segment2Angle = TailEntity.Segment2Angle;
            }

            DefaultGetHandler(getRequest);
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnChangeSegment1Angle(ChangeSegment1Angle angleRequest)
        {
			if (FaultIfNotConnected(angleRequest))
				return;

            TailEntity.Segment1Angle = angleRequest.Body.Angle;
            angleRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public void OnChangeSegment2Angle(ChangeSegment2Angle shoulderRequest)
        {
			if (FaultIfNotConnected(shoulderRequest))
				return;

            TailEntity.Segment2Angle = shoulderRequest.Body.Angle;
            shoulderRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OnPark(Park parkRequest)
        {
			if (FaultIfNotConnected(parkRequest))
				return;

            TailEntity.Segment1Angle = 0;
            TailEntity.Segment2Angle = MathHelper.PiOver2;
            parkRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

		protected override IConnectable GetState() { return _state; }

		TailEntity TailEntity { get { return Entity as TailEntity; } }
	}
}