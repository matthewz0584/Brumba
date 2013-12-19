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

		public void BatteryHttpGetHandler(HttpGet get)
		{
			UpdateStateFromSimulation();
			get.ResponsePort.Post(new HttpResponseType(_state.BatteryState));
		}

		public void BatteryGetHandler(battery.Get get)
        {
			UpdateStateFromSimulation();
			get.ResponsePort.Post(_state.BatteryState);
        }

        public void ReplaceHandler(battery.Replace replace)
        {
			_state.BatteryState = replace.Body;
            replace.ResponsePort.Post(DefaultReplaceResponseType.Instance);
            SendNotification(_subMgrPort, replace);
        }

        public IEnumerator<ITask> SubscribeHandler(battery.Subscribe subscribe)
        {
	        yield return SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort).Choice(success =>
		        {
			        UpdateStateFromSimulation();
			        SendNotificationToTarget(subscribe.Body.Subscriber, _subMgrPort, new battery.Replace {Body = _state.BatteryState});
		        }, EmptyHandler);
        }

        public void SetCriticalLevelHandler(battery.SetCriticalLevel request)
        {
			_state.BatteryState.PercentCriticalBattery = request.Body.PercentCriticalBattery;
            request.ResponsePort.Post(DefaultUpdateResponseType.Instance);
			SendNotification(_subMgrPort, request);
        }
    }
}
