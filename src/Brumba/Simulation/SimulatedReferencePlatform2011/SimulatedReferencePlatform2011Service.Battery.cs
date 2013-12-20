using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using battery = Microsoft.Robotics.Services.Battery;

namespace Brumba.Simulation.SimulatedReferencePlatform2011
{
    /// <summary>
    /// Reference Platform Service
    /// </summary>
    partial class SimulatedReferencePlatform2011Service
    {
        /// <summary>
        /// The battery port
        /// </summary>
        [AlternateServicePort("/Battery", AlternateContract = battery.Contract.Identifier, AllowMultipleInstances = true)]
        private battery.BatteryOperations _batteryPort = new battery.BatteryOperations();

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_batteryPort")]
		public void BatteryHttpGetHandler(HttpGet get)
		{
			if (Connected)
				UpdateStateFromSimulation();

			_state.Connected = Connected;
			get.ResponsePort.Post(new HttpResponseType(_state.BatteryState));
		}

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_batteryPort")]
		public void BatteryGetHandler(battery.Get get)
        {
			if (Connected)
				UpdateStateFromSimulation();

			_state.Connected = Connected;
			get.ResponsePort.Post(_state.BatteryState);
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_batteryPort")]
        public void ReplaceHandler(battery.Replace replace)
        {
			if (FaultIfNotConnected(replace))
				return;

			_state.BatteryState = replace.Body;
            replace.ResponsePort.Post(DefaultReplaceResponseType.Instance);
            SendNotification(_subMgrPort, replace);
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_batteryPort")]
        public IEnumerator<ITask> SubscribeHandler(battery.Subscribe subscribe)
        {
			if (FaultIfNotConnected(subscribe))
				yield break;

	        yield return SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort).Choice(success =>
		        {
			        UpdateStateFromSimulation();
			        SendNotificationToTarget(subscribe.Body.Subscriber, _subMgrPort, new battery.Replace {Body = _state.BatteryState});
		        }, EmptyHandler);
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_batteryPort")]
		public void SetCriticalLevelHandler(battery.SetCriticalLevel request)
        {
			if (FaultIfNotConnected(request))
				return;

			_state.BatteryState.PercentCriticalBattery = request.Body.PercentCriticalBattery;
            request.ResponsePort.Post(DefaultUpdateResponseType.Instance);
			SendNotification(_subMgrPort, request);
        }
    }
}
