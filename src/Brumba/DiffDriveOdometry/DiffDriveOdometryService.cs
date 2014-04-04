﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.DsspUtils;
using Brumba.WaiterStupid;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Robotics.Services.Drive.Proxy;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DiffDriveOdometry
{
	[Contract(Contract.Identifier)]
	[DisplayName("Brumba Differential Drive Odometry")]
	[Description("no description provided")]
	public class DiffDriveOdometryService : DsspServiceExposing
	{
		[ServiceState]
		[InitialStatePartner(Optional = false)]
		DiffDriveOdometryServiceState _state;

		[ServicePort("/Odometry", AllowMultipleInstances = true)]
		DiffDriveOdometryOperations _mainPort = new DiffDriveOdometryOperations();

		[Partner("DifferentialDrive", Contract = Microsoft.Robotics.Services.Drive.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		DriveOperations _diffDrive = new DriveOperations();

	    DiffDriveOdometryCalculator _diffDriveOdometryCalc;
		TimerFacade _timerFacade;

		public DiffDriveOdometryService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
            DC.Contract.Requires(creationPort != null);
		}

		protected override void Start()
		{
		    base.Start();

			_diffDriveOdometryCalc = new DiffDriveOdometryCalculator(_state.Constants);
			_timerFacade = new TimerFacade(this, _state.DeltaT);

			SpawnIterator(StartIt);
		}

		IEnumerator<ITask> StartIt()
		{
			yield return _diffDrive.Get().Receive(ds =>
				{
                    DC.Contract.Requires(ds.LeftWheel != null);
                    DC.Contract.Requires(ds.LeftWheel.EncoderState != null);
                    DC.Contract.Requires(ds.RightWheel != null);
                    DC.Contract.Requires(ds.RightWheel.EncoderState != null);

					_state.State.LeftTicks = ds.LeftWheel.EncoderState.CurrentReading;
					_state.State.RightTicks = ds.RightWheel.EncoderState.CurrentReading;
				});

			//To synchronize UpdateOdometry with Replace
			MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
					Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateOdometry))));

			yield return To.Exec(() => _timerFacade.Set());

		    //Activate(TimeoutPort(5000).Receive(_ => _timerFacade.Dispose()));
		}

		IEnumerator<ITask> UpdateOdometry(TimeSpan dt)
		{
			yield return _diffDrive.Get().Receive(ds =>
			{
                DC.Contract.Requires(ds.LeftWheel != null);
                DC.Contract.Requires(ds.LeftWheel.EncoderState != null);
                DC.Contract.Requires(ds.RightWheel != null);
                DC.Contract.Requires(ds.RightWheel.EncoderState != null);

                //LogInfo("Delta t {0}", dt.TotalSeconds);
				//LogInfo("Left wheel {0}", ds.LeftWheel.EncoderState.CurrentReading);
			    _state.State = _diffDriveOdometryCalc.UpdateOdometry(_state.State,
                    ds.LeftWheel.EncoderState.CurrentReading, ds.RightWheel.EncoderState.CurrentReading);
			});
		}

        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public void OnDropDown(DsspDefaultDrop dropDownRq)
        {
            DC.Contract.Requires(dropDownRq != null);
            DC.Contract.Requires(dropDownRq.Body != null);

            _timerFacade.Dispose();
            DefaultDropHandler(dropDownRq);
        }
	}
}