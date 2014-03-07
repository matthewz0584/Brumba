using System;
using System.Collections.Generic;
using Brumba.DsspUtils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;
using simTimerPxy = Brumba.Simulation.SimulatedTimer.Proxy;
using DC = System.Diagnostics.Contracts;

namespace Brumba.WaiterStupid
{
	public class TimerFacade : IDisposable
	{
		readonly DsspServiceExposing _srv;
		readonly float _interval;

		public TimerFacade(DsspServiceExposing srv, float interval)
		{
            DC.Contract.Requires(srv != null);
            DC.Contract.Requires(interval > 0);
            DC.Contract.Ensures(_srv != null);
            DC.Contract.Ensures(_interval > 0);
            DC.Contract.Ensures(TickPort != null);

			_srv = srv;
			_interval = interval;
			TickPort = new Port<TimeSpan>();
		}

		public Port<TimeSpan> TickPort { get; private set; }

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
		void StartSimulatedTimer(ServiceInfoType f)
		{
            DC.Contract.Requires(f != null);

			var simTimer = _srv.ServiceForwarder<simTimerPxy.SimulatedTimerOperations>(new Uri(f.Service));
		    _simTimerUnsubscribePort = new Port<Shutdown>();
		    simTimer.Post(
		        new simTimerPxy.Subscribe
		            {
		                Body = new simTimerPxy.SubscribeRequest(_interval),
		                NotificationPort = _simTimerNotificationPort,
		                NotificationShutdownPort = _simTimerUnsubscribePort
		            });

			_srv.Activate(_simTimerNotificationPort.P4.Receive(SimulationTimerHandler));
		}

	    void SimulationTimerHandler(simTimerPxy.Update simTimerUpdate)
	    {
            DC.Contract.Requires(simTimerUpdate != null);
            DC.Contract.Requires(simTimerUpdate.Body != null);

            if (_disposed)
                return;
	        TickPort.Post(IntervalToSpan(simTimerUpdate.Body.Delta));
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
            DC.Contract.Requires(interval >= 0);

			return new TimeSpan(0, 0, 0, 0, (int)(interval * 1000));
		}
	}
}
