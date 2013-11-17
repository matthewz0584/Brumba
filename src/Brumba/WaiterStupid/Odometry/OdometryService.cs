using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Services.Drive.Proxy;
using simTimerPxy = Brumba.Simulation.SimulatedTimer.Proxy;

namespace Brumba.WaiterStupid.Odometry
{
	[Contract(Contract.Identifier)]
	[DisplayName("Brumba diff drive odometry service")]
	[Description("no description provided")]
	public class OdometryService : DsspServiceBase
	{
		[ServiceState]
		OdometryServiceState _state = new OdometryServiceState
			{
				State = new OdometryState(),
				Constants = new OdometryConstants
				{
                    WheelBase = 0.3033f,
                    //WheelRadius = 0.0799846f, //The value from sim, it differs from physical characteristics, but sim service uses constant "MetersPerEncoderTick", that is probably acquired from manufacturer
                    TicksPerRotation = 36,
					//WheelBase = 0.406f,
					WheelRadius = 0.0762f,
					//TicksPerRotation = 144,
					DeltaT = 0.1f
				}
			};

		[ServicePort("/Odometry", AllowMultipleInstances = true)]
		OdometryOperations _mainPort = new OdometryOperations();

		[Partner("DiffDrive", Contract = Microsoft.Robotics.Services.Drive.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		DriveOperations _diffDrive = new DriveOperations();

        [Partner("SimTimer", Contract = simTimerPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        simTimerPxy.SimulatedTimerOperations _timer = new simTimerPxy.SimulatedTimerOperations();
        simTimerPxy.SimulatedTimerOperations _timerNotification = new simTimerPxy.SimulatedTimerOperations();

		Port<DateTime> _timerPort = new Port<DateTime>();

	    readonly OdometryCalculator _odometryCalc;

		public OdometryService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
			_odometryCalc = new OdometryCalculator { Constants = _state.Constants };
		}

		protected override void Start()
		{
		    base.Start();

			//1. Разобраться со временем: симулированным или нет
			//try
			//{
			//	ServiceForwarder<SimulatedTimerOperations>(String.Format(@"{0}://{1}/{2}", ServiceInfo.HttpServiceAlias.Scheme, ServiceInfo.HttpServiceAlias.Authority, "SimulatedTimer"));
			//}
			//catch (Exception)
			//{

			//}

            _timer.Subscribe(new simTimerPxy.SubscribeRequest { Interval = 0.1f }, _timerNotification);

			Activate(Arbiter.Receive(false, _diffDrive.Get(), 
				(DriveDifferentialTwoWheelState ds) =>
				{
					_state.State.LeftTicks = ds.LeftWheel.EncoderState.CurrentReading;
					_state.State.RightTicks = ds.RightWheel.EncoderState.CurrentReading;

                    //To synchronize with UpdateOdometry with UpdateConstants
					MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
                        Arbiter.ReceiveWithIterator<simTimerPxy.Update>(true, _timerNotification, UpdateOdometry))));
                    //    Arbiter.ReceiveWithIterator(true, _timerPort, UpdateOdometry))));

                    //TaskQueue.EnqueueTimer(DeltaTSpan, _timerPort);
				}));
		}

        IEnumerator<ITask> UpdateOdometry(simTimerPxy.Update dt)
        {
            yield return Arbiter.Receive(false, _diffDrive.Get(), (DriveDifferentialTwoWheelState ds) =>
            {
                _state.State = _odometryCalc.UpdateOdometry(_state.State, (int)(_state.Constants.DeltaT * 1000),
                                                            ds.LeftWheel.EncoderState.CurrentReading,
                                                            ds.RightWheel.EncoderState.CurrentReading);
                Console.WriteLine((DateTime.Now - _lastTime).Milliseconds);
                _lastTime = DateTime.Now;
            });
        }

	    DateTime _lastTime; 
		IEnumerator<ITask> UpdateOdometry(DateTime dt)
		{
			yield return Arbiter.Receive(false, _diffDrive.Get(), (DriveDifferentialTwoWheelState ds) =>
				{
                    _state.State = _odometryCalc.UpdateOdometry(_state.State, (float)(DateTime.Now - _lastTime).Milliseconds / 1000,
					                                            ds.LeftWheel.EncoderState.CurrentReading,
					                                            ds.RightWheel.EncoderState.CurrentReading);
				    Console.WriteLine((DateTime.Now - _lastTime).Milliseconds);
                    _lastTime = DateTime.Now;
					TaskQueue.EnqueueTimer(DeltaTSpan, _timerPort);
				});
		}

		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public void OnUpdateConstants(UpdateConstants updateConstantsRq)
		{
			_odometryCalc.Constants = _state.Constants = updateConstantsRq.Body;
			updateConstantsRq.ResponsePort.Post(DefaultUpdateResponseType.Instance);
		}

		TimeSpan DeltaTSpan
		{
			get { return new TimeSpan(0, 0, 0, 0, (int)(_state.Constants.DeltaT * 1000)); }
		}
	}
}
