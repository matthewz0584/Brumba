using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Xna.Framework;

namespace Brumba.Simulation.SimulatedStabilizer
{
	[Contract(Contract.Identifier)]
    [DisplayName("Simulated Stabilizer")]
    [Description("no description provided")]
	class SimulatedStabilizerService : DsspServiceBase
	{
		[ServiceState]
		SimulatedStabilizerState _state = new SimulatedStabilizerState();
		
		[ServicePort("/SimulatedStabilizer", AllowMultipleInstances = true)]
		SimulatedStabilizerOperations _mainPort = new SimulatedStabilizerOperations();

        SimulationEnginePort _simEngineNotifyPort = new SimulationEnginePort();
        StabilizerEntity _stabilizer;

        public SimulatedStabilizerService(DsspServiceCreationPort creationPort)
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

            _stabilizer = entity.Body as StabilizerEntity;
            _stabilizer.ServiceContract = Contract.Identifier;
            _state.Connected = true;

            SetUpForControlOfEntity();
        }

        void OnDeleteEntity(DeleteSimulationEntity entity)
        {
            LogInfo("SimulatedAckermanFourWheels OnDeleteEntity called");

            _stabilizer = null;
            _state.Connected = false;

            SetUpForWaitingForEntity();
        }

        void OnGet(Get getRequest)
        {
            if (_stabilizer != null)
            {
                _state.WheelToGroundDistances = _stabilizer.GroundRangefinders.Select(grf => grf.Distance).ToList();
                _state.TailAngle = _tailAngle;
	            _state.TailShoulder = _tailShoulder;
            }

            DefaultGetHandler(getRequest);
        }

	    float _tailAngle;
        void OnChangeTailAngle(ChangeTailAngle angleRequest)
        {
            _tailAngle = angleRequest.Body.Angle;
            _stabilizer.TailPosition = PolarToDecart(_tailAngle, _tailShoulder);            
            angleRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

	    float _tailShoulder;
        void OnChangeTailShoulder(ChangeTailShoulder shoulderRequest)
        {
            _tailShoulder = shoulderRequest.Body.Shoulder;
            _stabilizer.TailPosition = PolarToDecart(_tailAngle, _tailShoulder);
            shoulderRequest.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

	    Microsoft.Robotics.PhysicalModel.Vector2 PolarToDecart(float angle, float radius)
	    {
            return new Microsoft.Robotics.PhysicalModel.Vector2(
                radius * (float)Math.Cos(angle),
                radius * (float)Math.Sin(angle));
        }

        void OnPark(Park parkRequest)
        {
			_stabilizer.TailPosition = new Microsoft.Robotics.PhysicalModel.Vector2();
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
                        Arbiter.Receive<ChangeTailAngle>(true, _mainPort, OnChangeTailAngle),
                        Arbiter.Receive<ChangeTailShoulder>(true, _mainPort, OnChangeTailShoulder)
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