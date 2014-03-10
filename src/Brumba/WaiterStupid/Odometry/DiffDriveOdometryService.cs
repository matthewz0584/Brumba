using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.DsspUtils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Robotics.Services.Drive.Proxy;
using DC = System.Diagnostics.Contracts;

namespace Brumba.WaiterStupid.Odometry
{
	[Contract(Contract.Identifier)]
	[DisplayName("Brumba Differential Drive Odometry")]
	[Description("no description provided")]
	public class DiffDriveOdometryService : DsspServiceExposing
	{
		[ServiceState]
		DiffDriveOdometryServiceState _state = new DiffDriveOdometryServiceState
			{
				State = new DiffDriveOdometryState(),
				Constants = new DiffDriveOdometryConstants
				{
                    //WheelBase = 0.406f,
                    WheelBase = 0.3033f,
                    //WheelRadius = 0.0799846f, //The value from sim (now is replaced by 0.0762 in sim entity), it differs from physical characteristics, but sim service uses constant "MetersPerEncoderTick", that is probably acquired from manufacturer
                    WheelRadius = 0.0762f,
                    TicksPerRotation = 36,
					//TicksPerRotation = 144,
				},
                DeltaT = 0.1f
			};

		[ServicePort("/Odometry", AllowMultipleInstances = true)]
		DiffDriveOdometryOperations _mainPort = new DiffDriveOdometryOperations();

		[Partner("DifferentialDrive", Contract = Microsoft.Robotics.Services.Drive.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		DriveOperations _diffDrive = new DriveOperations();

	    readonly DiffDriveOdometryCalculator _diffDriveOdometryCalc;
		readonly TimerFacade _timerFacade;

		public DiffDriveOdometryService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
            DC.Contract.Requires(creationPort != null);

			_diffDriveOdometryCalc = new DiffDriveOdometryCalculator(_state.Constants);
			_timerFacade = new TimerFacade(this, _state.DeltaT);
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

        //ToDo:!!!Not tested at all!!! Test or remove
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public IEnumerator<ITask> OnReplace(Replace replaceRq) 
		{
            DC.Contract.Requires(replaceRq != null);
            DC.Contract.Requires(replaceRq.Body != null);
            DC.Contract.Requires(replaceRq.ResponsePort != null);
            DC.Contract.Requires(replaceRq.Body.DeltaT >= 0);

            yield return To.Exec(() => _timerFacade.Reset(replaceRq.Body.DeltaT));
		    _diffDriveOdometryCalc.Constants = replaceRq.Body.Constants;
            _state = replaceRq.Body;

			replaceRq.ResponsePort.Post(DefaultUpdateResponseType.Instance);
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
