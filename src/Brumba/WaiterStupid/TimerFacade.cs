using System;
using System.Collections.Generic;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;
using simTimerPxy = Brumba.Simulation.SimulatedTimer.Proxy;

namespace Brumba.WaiterStupid
{
	public class TimerFacade : IDisposable
	{
		private readonly DsspServiceExposing _srv;
		private readonly float _interval;

		public TimerFacade(DsspServiceExposing srv, float interval)
		{
			_srv = srv;
			_interval = interval;
			TickPort = new Port<TimeSpan>();
		}

		public Port<TimeSpan> TickPort { get; set; }

		public IEnumerator<ITask> Set()
		{
			var directoryResponcePort = _srv.DirectoryQuery(Simulation.SimulatedTimer.Proxy.Contract.Identifier, new TimeSpan(0, 0, 3));

			yield return Arbiter.Choice(
                _srv.TimeoutPort(1000).Receive(timeout => InternalTimerHandler(DateTime.Now)),
                directoryResponcePort.Receive((Fault fault) => InternalTimerHandler(DateTime.Now)),
				directoryResponcePort.Receive(serviceInfo => StartSimulatedTimer(serviceInfo)));
		}

	    bool _disposed;
        public void Dispose()
        {
            _disposed = true;
            if (_simTimerUnsubscribePort != null)
                _simTimerUnsubscribePort.Post(new Shutdown());
        }

		readonly simTimerPxy.SimulatedTimerOperations _simTimerNotificationPort = new simTimerPxy.SimulatedTimerOperations();
        Port<Shutdown> _simTimerUnsubscribePort;
        double _lastTime;
		void StartSimulatedTimer(ServiceInfoType f)
		{
			var simTimer = _srv.ServiceForwarder<simTimerPxy.SimulatedTimerOperations>(new Uri(f.Service));
		    _simTimerUnsubscribePort = new Port<Shutdown>();
		    simTimer.Post(
		        new simTimerPxy.Subscribe
		            {
		                Body = new simTimerPxy.SubscribeRequest(_interval),
		                NotificationPort = _simTimerNotificationPort,
		                NotificationShutdownPort = _simTimerUnsubscribePort
		            });

		    SimulationTimerHandler(new simTimerPxy.Update {Body = {ElapsedTime = 0}});
		}

	    void SimulationTimerHandler(simTimerPxy.Update simTimerUpdate)
	    {
            if (_disposed)
                return;
	        if (_lastTime != 0)
	            TickPort.Post(IntervalToSpan(simTimerUpdate.Body.ElapsedTime - _lastTime));
	        _lastTime = simTimerUpdate.Body.ElapsedTime;
            _srv.Activate(_simTimerNotificationPort.P4.Receive(SimulationTimerHandler));
	    }

	    DateTime _lastDate;
	    void InternalTimerHandler(DateTime time)
	    {
            if (_disposed)
                return;
	        if (_lastDate != new DateTime())
                TickPort.Post(time - _lastDate);
            _lastDate = time;
	        _srv.Activate(_srv.TimeoutPort(IntervalToSpan(_interval).Milliseconds).Receive(InternalTimerHandler));
	    }

	    static TimeSpan IntervalToSpan(double interval)
		{
			return new TimeSpan(0, 0, 0, 0, (int)(interval * 1000));
		}
	}
}
