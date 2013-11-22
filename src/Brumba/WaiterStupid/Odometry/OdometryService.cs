﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Robotics.Services.Drive.Proxy;

namespace Brumba.WaiterStupid.Odometry
{
	[Contract(Contract.Identifier)]
	[DisplayName("Brumba Differential Drive Odometry")]
	[Description("no description provided")]
	public class OdometryService : DsspServiceExposing
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

		[Partner("DifferentialDrive", Contract = Microsoft.Robotics.Services.Drive.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		DriveOperations _diffDrive = new DriveOperations();

	    readonly OdometryCalculator _odometryCalc;
		readonly TimerFacade _timerFacade;

		public OdometryService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
			_odometryCalc = new OdometryCalculator { Constants = _state.Constants };
			_timerFacade = new TimerFacade(this, _state.Constants.DeltaT);
		}

		protected override void Start()
		{
		    base.Start();

			SpawnIterator(StartIt);
		}

		IEnumerator<ITask> StartIt()
		{
			yield return _diffDrive.Get().Receive(ds =>
				{
					_state.State.LeftTicks = ds.LeftWheel.EncoderState.CurrentReading;
					_state.State.RightTicks = ds.RightWheel.EncoderState.CurrentReading;
				});

			//To synchronize UpdateOdometry with UpdateConstants
			MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
					Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateOdometry))));

			yield return To.Exec(() => _timerFacade.Set());
		}

		IEnumerator<ITask> UpdateOdometry(TimeSpan dt)
		{
			yield return _diffDrive.Get().Receive(ds =>
			{
				_state.State = _odometryCalc.UpdateOdometry(_state.State, dt.Milliseconds * 1000f,
															ds.LeftWheel.EncoderState.CurrentReading,
															ds.RightWheel.EncoderState.CurrentReading);
				//Console.WriteLine(dt.Milliseconds);
			});
		}

		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public void OnUpdateConstants(UpdateConstants updateConstantsRq) 
		{
			_odometryCalc.Constants = _state.Constants = updateConstantsRq.Body;
			updateConstantsRq.ResponsePort.Post(DefaultUpdateResponseType.Instance);
		}
	}
}