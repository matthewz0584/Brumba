using System;
using System.Collections.Generic;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;
using simTimerPxy = Brumba.Simulation.SimulatedTimer.Proxy;

namespace Brumba.WaiterStupid
{
	public class TimerFacade
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
				_srv.TimeoutPort(1000).Receive(timeout => StartInternalTimer()),
				directoryResponcePort.Receive((Fault f) => StartInternalTimer()),
				directoryResponcePort.Receive((ServiceInfoType f) => StartSimulatedTimer(f)));
		}

		readonly simTimerPxy.SimulatedTimerOperations _simTimerNotification = new simTimerPxy.SimulatedTimerOperations();
		double _lastTime;
		void StartSimulatedTimer(ServiceInfoType f)
		{
			var simTimer = _srv.ServiceForwarder<simTimerPxy.SimulatedTimerOperations>(f.HttpServiceAlias);
			simTimer.Subscribe(_interval, _simTimerNotification);
			_srv.Activate(Arbiter.Receive<simTimerPxy.Update>(true, _simTimerNotification, u =>
			{
				if (_lastTime != 0)
					TickPort.Post(IntervalToSpan(u.Body.ElapsedTime - _lastTime));
				_lastTime = u.Body.ElapsedTime;
			}));
		}

		readonly Port<DateTime> _timerPort = new Port<DateTime>();
		DateTime _lastDate;
		void StartInternalTimer()
		{
			_srv.Activate(Arbiter.Receive(true, _timerPort, dt =>
			{
				if (_lastDate != new DateTime())
					TickPort.Post(dt - _lastDate);
				_lastDate = dt;
				_srv.TaskQueue.EnqueueTimer(IntervalToSpan(_interval), _timerPort);
			}));
			_srv.TaskQueue.EnqueueTimer(IntervalToSpan(_interval), _timerPort);
		}

		static TimeSpan IntervalToSpan(double interval)
		{
			return new TimeSpan(0, 0, 0, 0, (int)(interval * 1000));
		}
	}
}
