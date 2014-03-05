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
	public class OdometryService : DsspServiceExposing
	{
		[ServiceState]
		OdometryServiceState _state = new OdometryServiceState
			{
				State = new OdometryState(),
				Constants = new OdometryConstants
				{
                    WheelBase = 0.3033f,
                    //WheelRadius = 0.0799846f, //The value from sim (now is replaced by 0.0762 in sim entity), it differs from physical characteristics, but sim service uses constant "MetersPerEncoderTick", that is probably acquired from manufacturer
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
            DC.Contract.Requires(creationPort != null);

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
                    DC.Contract.Requires(ds.LeftWheel != null);
                    DC.Contract.Requires(ds.LeftWheel.EncoderState != null);
                    DC.Contract.Requires(ds.RightWheel != null);
                    DC.Contract.Requires(ds.RightWheel.EncoderState != null);

					_state.State.LeftTicks = ds.LeftWheel.EncoderState.CurrentReading;
					_state.State.RightTicks = ds.RightWheel.EncoderState.CurrentReading;
				});

			//To synchronize UpdateOdometry with UpdateConstants
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

				_state.State = _odometryCalc.UpdateOdometry(_state.State, (float)dt.TotalSeconds,
															ds.LeftWheel.EncoderState.CurrentReading,
															ds.RightWheel.EncoderState.CurrentReading);
                //LogInfo("Delta t {0}", dt.TotalSeconds);
				//LogInfo("Velocity Z {0}", _state.State.Velocity.Z);
				//LogInfo("Left wheel {0}", ds.LeftWheel.EncoderState.CurrentReading);
			});
		}

		[ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public void OnUpdateConstants(UpdateConstants updateConstantsRq) 
		{
            DC.Contract.Requires(updateConstantsRq != null);
            DC.Contract.Requires(updateConstantsRq.Body != null);

			_odometryCalc.Constants = _state.Constants = updateConstantsRq.Body;
			updateConstantsRq.ResponsePort.Post(DefaultUpdateResponseType.Instance);
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
