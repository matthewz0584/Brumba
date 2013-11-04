// -----------------------------------------------------------------------
// <copyright file="SimulatedBattery.cs" company="Microsoft Corporation">
//      Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
//using Microsoft.Robotics.Services.Battery.Proxy;

using battery = Microsoft.Robotics.Services.Battery;
using submgr = Microsoft.Dss.Services.SubscriptionManager;


namespace Brumba.Simulation.SimulatedReferencePlatform2011
{
    /// <summary>
    /// Reference Platform Service
    /// </summary>
    partial class SimulatedReferencePlatform2011Service
    {
        /// <summary>
        /// The Battery Port Name
        /// </summary>
        private const string BatteryPortName = "_batteryPort";

        /// <summary>
        /// The battery port
        /// </summary>
        [AlternateServicePort("/battery", AlternateContract = battery.Contract.Identifier, AllowMultipleInstances = true)]
        private battery.BatteryOperations _batteryPort = new battery.BatteryOperations();

        /// <summary>
        /// The battery subscription manager
        /// </summary>
        [SubscriptionManagerPartner("BatterySubMgr")]
        private Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort batterySubMgr = new Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort();

        /// <summary>
        /// The battery state
        /// </summary>
        private battery.BatteryState batteryState = new battery.BatteryState
            { MaxBatteryPower = 12, PercentBatteryPower = 80, PercentCriticalBattery = 20 };

        /// <summary>
        /// The Get handler.
        /// </summary>
        /// <param name="get">The get request</param>
        //[ServiceHandler(PortFieldName = BatteryPortName)]
        public void GetHandler(battery.Get get)
        {
            get.ResponsePort.Post(this.batteryState);
        }

        /// <summary>
        /// The replace handler.
        /// </summary>
        /// <param name="replace">The replace handler</param>
        //[ServiceHandler(PortFieldName = BatteryPortName)]
        public void ReplaceHandler(battery.Replace replace)
        {
            this.batteryState = replace.Body;
            replace.ResponsePort.Post(DefaultReplaceResponseType.Instance);
            SendNotification(this.batterySubMgr, replace);
        }

        /// <summary>
        /// The subscribe handler.
        /// </summary>
        /// <param name="subscribe">The subscribe handler</param>
        /// <returns>Standard ccr iterator</returns>
        //[ServiceHandler(PortFieldName = BatteryPortName)]
        public IEnumerator<ITask> SubscribeHandler(battery.Subscribe subscribe)
        {
            yield return
                SubscribeHelper(batterySubMgr, subscribe.Body, subscribe.ResponsePort).Choice(
                    success =>
                    SendNotificationToTarget(
                        subscribe.Body.Subscriber, this.batterySubMgr, new battery.Replace { Body = this.batteryState }),
                    EmptyHandler);
        }

        /// <summary>
        /// The SetCriticalLevel handler.
        /// </summary>
        /// <param name="request">The request.</param>
        //[ServiceHandler(PortFieldName = BatteryPortName)]
        public void SetCriticalLevelHandler(battery.SetCriticalLevel request)
        {
            this.batteryState.PercentCriticalBattery = request.Body.PercentCriticalBattery;
            request.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            SendNotification(this.batterySubMgr, request);
        }
    }
}
