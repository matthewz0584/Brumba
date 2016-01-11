using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;
using timerPxy = Brumba.GenericTimer.Proxy;
using DC = System.Diagnostics.Contracts;

namespace Brumba.Common
{
	public class TimerFacade : IDisposable
	{
		readonly DsspServiceExposing _srv;

		[DC.ContractInvariantMethod]
	    void ObjectInvariant()
	    {
	        DC.Contract.Invariant(Interval > 0);
			DC.Contract.Invariant(_srv != null);
	    }

		public TimerFacade(DsspServiceExposing srv, float interval)
		{
            DC.Contract.Requires(srv != null);
            DC.Contract.Requires(interval > 0);
            DC.Contract.Ensures(_srv != null);
            DC.Contract.Ensures(Interval > 0);
            DC.Contract.Ensures(TickPort != null);

			_srv = srv;
			Interval = interval;
			TickPort = new Port<TimeSpan>();
		}

		public Port<TimeSpan> TickPort { get; private set; }
		public float Interval { get; private set; }

		public IEnumerator<ITask> Set()
		{
            var directoryResponcePort = _srv.DirectoryQuery(timerPxy.Contract.Identifier, new TimeSpan(0, 0, 3));

			yield return Arbiter.Choice(
                _srv.TimeoutPort(1000).Receive(timeout => InternalTimerHandler(DateTime.Now)),
                directoryResponcePort.Receive((Fault fault) => InternalTimerHandler(DateTime.Now)),
				directoryResponcePort.Receive(serviceInfo => StartExternalTimer(serviceInfo)));
		}

        //ToDo:!!!Not tested at all!!! Test or remove
		/// <summary>
		/// Not tested
		/// </summary>
		/// <param name="interval"></param>
		/// <returns></returns>
        public IEnumerator<ITask> Reset(float interval)
        {
            DC.Contract.Requires(interval > 0);
            DC.Contract.Ensures(Interval > 0);

            Dispose();
            _disposed = false;
            Interval = interval;
            yield return To.Exec(Set);
        }

	    bool _disposed;
        public void Dispose()
        {
            DC.Contract.Ensures(_disposed);

            _disposed = true;
            if (_simTimerUnsubscribePort != null)
                _simTimerUnsubscribePort.Post(new Shutdown());
        }

        readonly timerPxy.TimerOperations _simTimerNotificationPort = new timerPxy.TimerOperations();
        Port<Shutdown> _simTimerUnsubscribePort;
		void StartExternalTimer(ServiceInfoType f)
		{
            DC.Contract.Requires(f != null);
            DC.Contract.Ensures(_simTimerUnsubscribePort != null);

            var simTimer = _srv.ServiceForwarder<timerPxy.TimerOperations>(new Uri(f.Service));
		    _simTimerUnsubscribePort = new Port<Shutdown>();
		    simTimer.Post(
                new timerPxy.Subscribe
		            {
                        Body = new timerPxy.SubscribeRequest(Interval),
		                NotificationPort = _simTimerNotificationPort,
		                NotificationShutdownPort = _simTimerUnsubscribePort
		            });

			_srv.Activate(_simTimerNotificationPort.P4.Receive(ExternalTimerHandler));
		}

        void ExternalTimerHandler(timerPxy.Update simTimerUpdate)
	    {
            DC.Contract.Requires(simTimerUpdate != null);
            DC.Contract.Requires(simTimerUpdate.Body != null);

            DC.Contract.Assert(simTimerUpdate.Body.Delta > 0);

            if (_disposed)
                return;
	        TickPort.Post(IntervalToSpan(simTimerUpdate.Body.Delta));
            _srv.Activate(_simTimerNotificationPort.P4.Receive(ExternalTimerHandler));
	    }

	    DateTime _lastDate;
	    void InternalTimerHandler(DateTime time)
	    {
            if (_disposed)
                return;
	        if (_lastDate != new DateTime())
                TickPort.Post(time - _lastDate);
            _lastDate = time;
	        _srv.Activate(_srv.TimeoutPort(IntervalToSpan(Interval).Milliseconds).Receive(InternalTimerHandler));
	    }

	    static TimeSpan IntervalToSpan(double interval)
		{
            DC.Contract.Requires(interval >= 0);

			return new TimeSpan(0, 0, 0, 0, (int)(interval * 1000));
		}
	}
}
