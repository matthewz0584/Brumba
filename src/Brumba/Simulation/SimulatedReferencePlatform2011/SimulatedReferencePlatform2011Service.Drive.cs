using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Robotics.Simulation.Engine;
using W3C.Soap;
using drive = Microsoft.Robotics.Services.Drive;

namespace Brumba.Simulation.SimulatedReferencePlatform2011
{
    partial class SimulatedReferencePlatform2011Service
    {
        /// <summary>
        /// The drive operation port
        /// </summary>
        [AlternateServicePort("/DifferentialDrive", AllowMultipleInstances = true, AlternateContract = drive.Contract.Identifier)]
		private drive.DriveOperations _drivePort = new drive.DriveOperations();

        /// <summary>
        /// The encoder value of the left wheel at last reset
        /// </summary>
        private int lastResetLeftWheelEncoderValue;

        /// <summary>
        /// The encoder value of the right wheel at last reset
        /// </summary>
        private int lastResetRightWheelEncoderValue;

        public void DriveHttpGetHandler(HttpGet get)
        {
            UpdateStateFromSimulation();
            get.ResponsePort.Post(new HttpResponseType(_state.DriveState));
        }

		public void DriveGetHandler(drive.Get get)
        {
            UpdateStateFromSimulation();
            get.ResponsePort.Post(_state.DriveState);
        }

		public IEnumerator<ITask> DriveSubscribeHandler(drive.Subscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
                _subMgrPort.Post(
                    new Microsoft.Dss.Services.SubscriptionManager.Submit(subscribe.Body.Subscriber, DsspActions.UpdateRequest, _state.DriveState, null)),
                LogError);
        }

		public IEnumerator<ITask> DriveReliableSubscribeHandler(drive.ReliableSubscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
                _subMgrPort.Post(
                    new Microsoft.Dss.Services.SubscriptionManager.Submit(subscribe.Body.Subscriber, DsspActions.UpdateRequest, _state.DriveState, null)),
                LogError);
        }

		public void ResetEncodersHandler(drive.ResetEncoders resetEncoders)
        {
            lastResetLeftWheelEncoderValue += _state.DriveState.LeftWheel.EncoderState.CurrentReading;
            lastResetRightWheelEncoderValue += _state.DriveState.RightWheel.EncoderState.CurrentReading;
            resetEncoders.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        public void DriveDistanceHandler(drive.DriveDistance driveDistance)
        {
            if (!EnableCheck(driveDistance.ResponsePort, "DriveDistance")) return;

            if ((driveDistance.Body.Power > 1.0f) || (driveDistance.Body.Power < -1.0f))
            {
                // invalid drive power
                driveDistance.ResponsePort.Post(Fault.FromException(new Exception("Invalid Power parameter.")));
                LogError("Invalid Power parameter in DriveDistanceHandler."); 
                return;
            }

            _state.DriveState.DriveDistanceStage = driveDistance.Body.DriveDistanceStage;
			if (driveDistance.Body.DriveDistanceStage == drive.DriveStage.InitialRequest)
            {
                var entityResponse = new Port<OperationResult>();
                Activate(
                    Arbiter.Receive(
                        false,
                        entityResponse,
                        result =>
                            {
                                // post a message to ourselves indicating that the drive distance has completed
								var req = new drive.DriveDistanceRequest(0, 0);
                                switch (result)
                                {
                                    case OperationResult.Error:
										req.DriveDistanceStage = drive.DriveStage.Canceled;
                                        break;
                                    case OperationResult.Canceled:
										req.DriveDistanceStage = drive.DriveStage.Canceled;
                                        break;
                                    case OperationResult.Completed:
										req.DriveDistanceStage = drive.DriveStage.Completed;
                                        break;
                                }

								_drivePort.Post(new drive.DriveDistance(req));
                            }));

                RpEntity.DriveDistance((float)driveDistance.Body.Distance, (float)driveDistance.Body.Power, entityResponse);

				var req2 = new drive.DriveDistanceRequest(0, 0) { DriveDistanceStage = drive.DriveStage.Started };
				_drivePort.Post(new drive.DriveDistance(req2));
            }
            else
            {
                SendNotification(_subMgrPort, driveDistance);
            }

            driveDistance.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

	    public void DriveRotateHandler(drive.RotateDegrees rotate)
        {
			if (!EnableCheck(rotate.ResponsePort, "RotateDegrees")) return;

            _state.DriveState.RotateDegreesStage = rotate.Body.RotateDegreesStage;
			if (rotate.Body.RotateDegreesStage == drive.DriveStage.InitialRequest)
            {
                var entityResponse = new Port<OperationResult>();
                Activate(
                    Arbiter.Receive(
                        false,
                        entityResponse,
                        result =>
                            {
                                // post a message to ourselves indicating that the drive distance has completed
								var req = new drive.RotateDegreesRequest(0, 0);
                                switch (result)
                                {
                                    case OperationResult.Error:
										req.RotateDegreesStage = drive.DriveStage.Canceled;
                                        break;
                                    case OperationResult.Canceled:
										req.RotateDegreesStage = drive.DriveStage.Canceled;
                                        break;
                                    case OperationResult.Completed:
										req.RotateDegreesStage = drive.DriveStage.Completed;
                                        break;
                                }

								_drivePort.Post(new drive.RotateDegrees(req));
                            }));

                RpEntity.RotateDegrees((float)rotate.Body.Degrees, (float)rotate.Body.Power, entityResponse);

				var req2 = new drive.RotateDegreesRequest(0, 0) { RotateDegreesStage = drive.DriveStage.Started };
				_drivePort.Post(new drive.RotateDegrees(req2));
            }
            else
            {
                SendNotification(_subMgrPort, rotate);
            }

            rotate.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

		public void DriveSetPowerHandler(drive.SetDrivePower setPower)
        {
			if (!EnableCheck(setPower.ResponsePort, "SetPower")) return;

            if ((setPower.Body.LeftWheelPower > 1.0f) || (setPower.Body.LeftWheelPower < -1.0f) ||
                (setPower.Body.RightWheelPower > 1.0f) || (setPower.Body.RightWheelPower < -1.0f))
            {
                // invalid drive power
                setPower.ResponsePort.Post(Fault.FromException(new Exception("Invalid Power parameter.")));
                LogError("Invalid Power parameter in SetPowerHandler.");
                return;
            }

            // Call simulation entity method for setting wheel torque
            RpEntity.SetMotorTorque((float)setPower.Body.LeftWheelPower, (float)setPower.Body.RightWheelPower);

            UpdateStateFromSimulation();
            setPower.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update notification for entire state
            _subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(_state.DriveState, DsspActions.UpdateRequest));
        }

		public void DriveSetSpeedHandler(drive.SetDriveSpeed setSpeed)
        {
			if (!EnableCheck(setSpeed.ResponsePort, "SetSpeed")) return;

            RpEntity.SetVelocity((float)setSpeed.Body.LeftWheelSpeed, (float)setSpeed.Body.RightWheelSpeed);

            UpdateStateFromSimulation();
            setSpeed.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update notification for entire state
            _subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(_state.DriveState, DsspActions.UpdateRequest));
        }

		public void DriveEnableHandler(drive.EnableDrive enable)
        {
            _state.DriveState.IsEnabled = enable.Body.Enable;
            RpEntity.IsEnabled = _state.DriveState.IsEnabled;

            UpdateStateFromSimulation();
            enable.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update for entire state
            _subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(_state.DriveState, DsspActions.UpdateRequest));
        }

		public void DriveAllStopHandler(drive.AllStop estop)
        {
            RpEntity.SetMotorTorque(0, 0);
            RpEntity.SetVelocity(0);

            // AllStop disables the drive
            RpEntity.IsEnabled = false;

            UpdateStateFromSimulation();
            estop.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update for entire state
            _subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(_state.DriveState, DsspActions.UpdateRequest));
        }

        /// <summary>
        /// Rotations to ticks.
        /// </summary>
        /// <param name="wheel">The wheel entity.</param>
        /// <returns>The number of rotations converted to ticks</returns>
        private static int RotationsToTicks(WheelEntity wheel)
        {
            const double MetersPerEncoderTick = 0.01328;

            return (int)(wheel.Rotations * 2 * Math.PI * wheel.Wheel.State.Radius / MetersPerEncoderTick);
        }

        /// <summary>
        /// Updates the state from simulation.
        /// </summary>
        private void UpdateStateFromSimulation()
        {
	        _state.DriveState.TimeStamp = DateTime.Now;

	        // Reverse out the encoder ticks
	        _state.DriveState.LeftWheel.EncoderState.TimeStamp = _state.DriveState.TimeStamp;
	        _state.DriveState.LeftWheel.EncoderState.CurrentReading =
		        RotationsToTicks(RpEntity.LeftWheel) - lastResetLeftWheelEncoderValue;
	        _state.DriveState.RightWheel.EncoderState.CurrentReading =
		        RotationsToTicks(RpEntity.RightWheel) - lastResetRightWheelEncoderValue;

	        // Compute the wheel speeds
	        _state.DriveState.LeftWheel.WheelSpeed = -RpEntity.LeftWheel.Wheel.AxleSpeed
	                                                      * RpEntity.LeftWheel.Wheel.State.Radius;
	        _state.DriveState.RightWheel.WheelSpeed = -RpEntity.RightWheel.Wheel.AxleSpeed
	                                                       * RpEntity.RightWheel.Wheel.State.Radius;

	        // Compute the power
	        _state.DriveState.LeftWheel.MotorState.CurrentPower = RpEntity.LeftWheel.Wheel.MotorTorque;
	        _state.DriveState.RightWheel.MotorState.CurrentPower = RpEntity.RightWheel.Wheel.MotorTorque;
	        _state.DriveState.IsEnabled = RpEntity.IsEnabled;
        }

		private bool EnableCheck(PortSet<DefaultUpdateResponseType, Fault> responsePort, string operation)
		{
			if (_state.DriveState.IsEnabled)
				return true;

			responsePort.Post(Fault.FromException(new Exception("Drive is not enabled.")));
			LogError(operation + " request to disabled drive.");
			return false;
		}
    }
}