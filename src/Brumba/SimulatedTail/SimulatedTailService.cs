using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Xna.Framework;

namespace Brumba.Simulation.SimulatedTail
{
	[Contract(Contract.Identifier)]
    [DisplayName("Simulated tail")]
    [Description("no description provided")]
	class SimulatedTailService : DsspServiceBase
	{
		[ServiceState]
		SimulatedTailState _state = new SimulatedTailState();
		
		[ServicePort("/SimulatedTail", AllowMultipleInstances = true)]
		SimulatedTailOperations _mainPort = new SimulatedTailOperations();

        SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        TailEntity _tail;

        public SimulatedTailService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}

		protected override void Start()
		{
            SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _simEngineNotifyPort);

            base.Start();

            //Killing default main port interleave. From now on I control main port interleave
            MainPortInterleave.CombineWith(new Interleave(
                    new TeardownReceiverGroup(Arbiter.Receive<Park>(false, _mainPort, b => { })),
                    new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup()));
            _mainPort.Post(new Park());

            SetUpForWaitingForEntity(); 
		}

        void OnInsertEntity(InsertSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanFourWheels OnInsertEntity called");

            _tail = entity.Body as TailEntity;
            _tail.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanFourWheels OnDeleteEntity called");

            _tail = null;
            _state.Connected = false;

            SetUpForWaitingForEntity();
        }

        void OnGet(Get getRequest)
        {
            if (_tail != null)
            {
                _state.WheelToGroundDistances = _tail.GroundRangefinders.Select(grf => grf.Distance).ToList();
                _state.Segment1Angle = _tail.Segment1Angle;
	            _state.Segment2Angle = _tail.Segment2Angle;
            }

            DefaultGetHandler(getRequest);
        }

        void OnChangeSegment1Angle(ChangeSegment1Angle angleRequest)
        {
            _tail.Segment1Angle = angleRequest.Body.Angle;
            angleRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnChangeSegment2Angle(ChangeSegment2Angle shoulderRequest)
        {
            _tail.Segment2Angle = shoulderRequest.Body.Angle;
            shoulderRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        void OnPark(Park parkRequest)
        {
			_tail.Segment1Angle = 0;
            _tail.Segment2Angle = MathHelper.PiOver2;
            parkRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
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
                        Arbiter.Receive<Park>(true, _mainPort, OnPark),
                        Arbiter.Receive<ChangeSegment1Angle>(true, _mainPort, OnChangeSegment1Angle),
                        Arbiter.Receive<ChangeSegment2Angle>(true, _mainPort, OnChangeSegment2Angle)
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