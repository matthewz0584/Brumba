using System.ComponentModel;
using System.Linq;
using Microsoft.Ccr.Core;
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
		SimulatedTailState _state = new SimulatedTailState();
		
		[ServicePort("/SimulatedTail", AllowMultipleInstances = true)]
		SimulatedTailOperations _mainPort = new SimulatedTailOperations();

        public SimulatedTailService(DsspServiceCreationPort creationPort)
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
	                Arbiter.Receive<Park>(true, _mainPort, OnPark),
	                Arbiter.Receive<ChangeSegment1Angle>(true, _mainPort, OnChangeSegment1Angle),
	                Arbiter.Receive<ChangeSegment2Angle>(true, _mainPort, OnChangeSegment2Angle)
	                ),
	            new ConcurrentReceiverGroup(
	                Arbiter.Receive<DsspDefaultLookup>(true, _mainPort, DefaultLookupHandler),
	                Arbiter.Receive<Get>(true, _mainPort, OnGet)
	                ));
	    }

        void OnGet(Get getRequest)
        {
	        _state.Connected = Connected;

            if (TailEntity != null)
            {
                _state.WheelToGroundDistances = TailEntity.GroundRangefinders.Select(grf => grf.Distance).ToList();
                _state.Segment1Angle = TailEntity.Segment1Angle;
                _state.Segment2Angle = TailEntity.Segment2Angle;
            }

            DefaultGetHandler(getRequest);
        }

        void OnChangeSegment1Angle(ChangeSegment1Angle angleRequest)
        {
            TailEntity.Segment1Angle = angleRequest.Body.Angle;
            angleRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnChangeSegment2Angle(ChangeSegment2Angle shoulderRequest)
        {
            TailEntity.Segment2Angle = shoulderRequest.Body.Angle;
            shoulderRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnPark(Park parkRequest)
        {
            TailEntity.Segment1Angle = 0;
            TailEntity.Segment2Angle = MathHelper.PiOver2;
            parkRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        TailEntity TailEntity { get { return Entity as TailEntity; } }
	}
}