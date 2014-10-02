using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.DsspUtils;
using Brumba.Utils;
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
#pragma warning disable 0649
		[ServiceState]
		[InitialStatePartner(Optional = false)]
		DiffDriveOdometryServiceState _state;
#pragma warning restore 0649

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
		    (new Pose() as IFreezable).Freeze(); //real usage of Utils assembly, VS can not strip reference
		}

		protected override void Start()
		{
			SpawnIterator(StartIt);
		}

		IEnumerator<ITask> StartIt()
		{
			_timerFacade = new TimerFacade(this, _state.DeltaT);
			_diffDriveOdometryCalc = new DiffDriveOdometryCalculator(_state.Constants);

			yield return _diffDrive.Get().Receive(ds =>
				{
                    DC.Contract.Requires(ds.LeftWheel != null);
                    DC.Contract.Requires(ds.LeftWheel.EncoderState != null);
                    DC.Contract.Requires(ds.RightWheel != null);
                    DC.Contract.Requires(ds.RightWheel.EncoderState != null);

					_state.State.LeftTicks = ds.LeftWheel.EncoderState.CurrentReading;
					_state.State.RightTicks = ds.RightWheel.EncoderState.CurrentReading;
				});

			base.Start();

			//To synchronize UpdateOdometry with Replace
			MainPortInterleave.CombineWith(new Interleave(new ExclusiveReceiverGroup(), new ConcurrentReceiverGroup(
					Arbiter.ReceiveWithIterator(true, _timerFacade.TickPort, UpdateOdometry))));

			yield return To.Exec(() => _timerFacade.Set());
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
			    _state.State = _diffDriveOdometryCalc.UpdateOdometry(_state.State, dt.TotalSeconds,
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
