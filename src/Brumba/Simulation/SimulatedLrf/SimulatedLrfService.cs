using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Physics;
using System.ComponentModel;
using Microsoft.Dss.Core.DsspHttp;
using System.Net;

namespace Brumba.Simulation.SimulatedLrf
{
	/// <summary>
	/// Provides access to a simulated Laser Range Finder contract
	/// using physics raycasting and the LaserRangeFinderEntity
	/// </summary>
	[DisplayName("Simulated Laser Range Finder")]
	[Description("Provides access to a simulated laser range finder.\n(Uses the Sick Laser Range Finder contract.)")]
	[AlternateContract(Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Contract.Identifier)]
	[Contract(Contract.Identifier)]
	public class SimulatedLrfService : DsspServiceBase
	{
		#region Simulation Variables
		Microsoft.Robotics.Simulation.Engine.LaserRangeFinderEntity _entity;
		Microsoft.Robotics.Simulation.Engine.SimulationEnginePort _notificationTarget;
		Port<RaycastResult> _raycastResults = new Port<RaycastResult>();
		#endregion

		private Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.State _state = new Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.State();

		[ServicePort("/SimulatedLrf", AllowMultipleInstances = true)]
		private Operations _mainPort = new Operations();

		[AlternateServicePort("/SickLRF", AllowMultipleInstances = true, AlternateContract = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Contract.Identifier)]
		private Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.SickLRFOperations _sickLrfPort = new Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.SickLRFOperations();

		[Partner("SubMgr", Contract = Microsoft.Dss.Services.SubscriptionManager.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
		Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort _subMgrPort = new Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort();

		/// <summary>
		/// SimulatedLRFService constructor that takes a PortSet to notify when the service is created
		/// </summary>
		/// <param name="creationPort"></param>
		public SimulatedLrfService(Microsoft.Dss.ServiceModel.Dssp.DsspServiceCreationPort creationPort) :
			base(creationPort)
		{
		}

		/// <summary>
		/// Start initializes SimulatedLRFService and listens for drop messages
		/// </summary>
		protected override void Start()
		{
			_notificationTarget = new Microsoft.Robotics.Simulation.Engine.SimulationEnginePort();

			// PartnerType.Service is the entity instance name.
			Microsoft.Robotics.Simulation.Engine.SimulationEngine.GlobalInstancePort.Subscribe(ServiceInfo.PartnerList, _notificationTarget);


			// dont start listening to DSSP operations, other than drop, until notification of entity
			Activate(new Interleave(
				new TeardownReceiverGroup
				(
					Arbiter.Receive<Microsoft.Robotics.Simulation.Engine.InsertSimulationEntity>(false, _notificationTarget, InsertEntityNotificationHandlerFirstTime),
					Arbiter.Receive<Microsoft.Dss.ServiceModel.Dssp.DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)
				),
				new ExclusiveReceiverGroup(),
				new ConcurrentReceiverGroup()
			));

		}

		void DeleteEntityNotificationHandler(Microsoft.Robotics.Simulation.Engine.DeleteSimulationEntity del)
		{
			_entity = null;
		}

		void InsertEntityNotificationHandlerFirstTime(Microsoft.Robotics.Simulation.Engine.InsertSimulationEntity ins)
		{
			InsertEntityNotificationHandler(ins);
			base.Start();
			MainPortInterleave.CombineWith(
				new Interleave(
					new TeardownReceiverGroup(),
					new ExclusiveReceiverGroup(
						Arbiter.Receive<Microsoft.Robotics.Simulation.Engine.InsertSimulationEntity>(true, _notificationTarget, InsertEntityNotificationHandler),
						Arbiter.Receive<Microsoft.Robotics.Simulation.Engine.DeleteSimulationEntity>(true, _notificationTarget, DeleteEntityNotificationHandler)
					),
					new ConcurrentReceiverGroup()
				)
			);
		}

		void InsertEntityNotificationHandler(Microsoft.Robotics.Simulation.Engine.InsertSimulationEntity ins)
		{
			_entity = (Microsoft.Robotics.Simulation.Engine.LaserRangeFinderEntity)ins.Body;
			_entity.ServiceContract = Contract.Identifier;

			_state.Units = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Units.Millimeters;
			_state.AngularRange = (int)Math.Abs(_entity.RaycastProperties.EndAngle - _entity.RaycastProperties.StartAngle);
			_state.AngularResolution = _entity.RaycastProperties.AngleIncrement;

			try
			{
				_entity.Register(_raycastResults);
			}
			catch (Exception ex)
			{
				LogError(ex);
			}

			// attach handler to raycast results port
			Activate(Arbiter.Receive(true, _raycastResults, RaycastResultsHandler));
		}

		private void RaycastResultsHandler(Microsoft.Robotics.Simulation.Physics.RaycastResult result)
		{
			// we just receive ray cast information from physics. Currently we just use
			// the distance measurement for each impact point reported. However, our simulation
			// engine also provides you the material properties so you can decide here to simulate
			// scattering, reflections, noise etc.

			Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.State latestResults = new Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.State();
			latestResults.DistanceMeasurements = new int[result.SampleCount + 1];
			int initValue = (int)(_entity.RaycastProperties.Range * 1000f);
			for (int i = 0; i < (result.SampleCount + 1); i++)
				latestResults.DistanceMeasurements[i] = initValue;

			foreach (Microsoft.Robotics.Simulation.Physics.RaycastImpactPoint pt in result.ImpactPoints)
			{
				// the distance to the impact has been pre-calculted from the origin
				// and it's in the fourth element of the vector
				latestResults.DistanceMeasurements[pt.ReadingIndex] = (int)(pt.Position.W * 1000f);
			}

			latestResults.AngularRange = (int)Math.Abs(_entity.RaycastProperties.EndAngle - _entity.RaycastProperties.StartAngle);
			latestResults.AngularResolution = _entity.RaycastProperties.AngleIncrement;
			latestResults.Units = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Units.Millimeters;
			latestResults.LinkState = "Measurement received";
			latestResults.TimeStamp = DateTime.Now;

			// send replace message to self
			Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Replace replace = new Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Replace();
			// for perf reasons dont set response port, we are just talking to ourself anyway
			replace.ResponsePort = null;
			replace.Body = latestResults;
			_sickLrfPort.Post(replace);
		}

		/// <summary>
		/// Get the SimulatedLRF state
		/// </summary>
		/// <param name="get"></param>
		/// <returns></returns>
		[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public IEnumerator<ITask> GetHandler(Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Get get)
		{
			get.ResponsePort.Post(_state);
			yield break;
		}

		/// <summary>
		/// Get the SimulatedLRF state
		/// </summary>
		/// <param name="get"></param>
		/// <returns></returns>
		[ServiceHandler(ServiceHandlerBehavior.Concurrent)]
		public IEnumerator<ITask> GetHandler(HttpGet get)
		{
			get.ResponsePort.Post(new HttpResponseType(HttpStatusCode.OK, _state));
			yield break;
		}

		/// <summary>
		/// Processes a replace message
		/// </summary>
		/// <param name="replace"></param>
		/// <returns></returns>
		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public IEnumerator<ITask> ReplaceHandler(Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Replace replace)
		{
			_state = replace.Body;
			if (replace.ResponsePort != null)
				replace.ResponsePort.Post(Microsoft.Dss.ServiceModel.Dssp.DefaultReplaceResponseType.Instance);

			// issue notification
			_subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(_state, Microsoft.Dss.ServiceModel.Dssp.DsspActions.ReplaceRequest));
			yield break;
		}

		/// <summary>
		/// Subscribe to SimulatedLRF service
		/// </summary>
		/// <param name="subscribe"></param>
		/// <returns></returns>
		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public IEnumerator<ITask> SubscribeHandler(Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Subscribe subscribe)
		{
			yield return Arbiter.Choice(
				SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
				delegate(SuccessResult success)
				{
					_subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(
						subscribe.Body.Subscriber, Microsoft.Dss.ServiceModel.Dssp.DsspActions.ReplaceRequest, _state, null));
				},
				delegate(Exception ex) { LogError(ex); }
			);
		}

		/// <summary>
		/// Subscribe to SimulatedLRF service
		/// </summary>
		/// <param name="subscribe"></param>
		/// <returns></returns>
		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public IEnumerator<ITask> ReliableSubscribeHandler(Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.ReliableSubscribe subscribe)
		{
			yield return Arbiter.Choice(
				SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
				delegate(SuccessResult success)
				{
					_subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(
						subscribe.Body.Subscriber, Microsoft.Dss.ServiceModel.Dssp.DsspActions.ReplaceRequest, _state, null));
				},
				delegate(Exception ex) { LogError(ex); }
			);
		}
	}
}
