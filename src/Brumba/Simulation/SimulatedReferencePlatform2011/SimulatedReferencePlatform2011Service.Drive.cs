using System;
using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Robotics.Simulation.Engine;
using W3C.Soap;
using Drive = Microsoft.Robotics.Services.Drive;
using SubMngr = Microsoft.Dss.Services.SubscriptionManager;

namespace Brumba.Simulation.SimulatedReferencePlatform2011
{
    partial class SimulatedReferencePlatform2011Service
    {
        /// <summary>
        /// The drive operation port
        /// </summary>
        [AlternateServicePort("/DifferentialDrive", AllowMultipleInstances = true, AlternateContract = Drive.Contract.Identifier)]
		Drive.DriveOperations _drivePort = new Drive.DriveOperations();

        /// <summary>
        /// The encoder value of the left wheel at last reset
        /// </summary>
        private int _lastResetLeftWheelEncoderValue;

        /// <summary>
        /// The encoder value of the right wheel at last reset
        /// </summary>
        private int _lastResetRightWheelEncoderValue;

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
		public void DriveHttpGetHandler(HttpGet get)
        {
			if (IsConnected)
				UpdateStateFromSimulation();

            get.ResponsePort.Post(new HttpResponseType(_state.DriveState));
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
		public void DriveGetHandler(Drive.Get get)
        {
			if (IsConnected)
				 UpdateStateFromSimulation();

            get.ResponsePort.Post(_state.DriveState);
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
		public IEnumerator<ITask> DriveSubscribeHandler(Drive.Subscribe subscribe)
        {
			if (FaultIfNotConnected(subscribe))
				yield break;

            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
	                {
						UpdateStateFromSimulation();
						_subMgrPort.Post(new SubMngr.Submit(subscribe.Body.Subscriber, DsspActions.UpdateRequest, _state.DriveState, null));
	                },
                LogError);
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
		public IEnumerator<ITask> DriveReliableSubscribeHandler(Drive.ReliableSubscribe subscribe)
        {
			if (FaultIfNotConnected(subscribe))
				yield break;

            yield return Arbiter.Choice(
                SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
	                {
		                UpdateStateFromSimulation();
		                _subMgrPort.Post(new SubMngr.Submit(subscribe.Body.Subscriber, DsspActions.UpdateRequest, _state.DriveState, null));
	                },
                LogError);
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_drivePort")]
		public void ResetEncodersHandler(Drive.ResetEncoders resetEncoders)
        {
			if (FaultIfNotConnected(resetEncoders))
				return;

            _lastResetLeftWheelEncoderValue += _state.DriveState.LeftWheel.EncoderState.CurrentReading;
            _lastResetRightWheelEncoderValue += _state.DriveState.RightWheel.EncoderState.CurrentReading;
            resetEncoders.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_drivePort")]
		public void DriveDistanceHandler(Drive.DriveDistance driveDistance)
        {
            driveDistance.ResponsePort.Post(Fault.FromException(new Exception("DriveDistance is not implemented.")));
            LogError("DriveDistance is not implemented.");
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_drivePort")]
	    public void DriveRotateHandler(Drive.RotateDegrees rotate)
        {
            rotate.ResponsePort.Post(Fault.FromException(new Exception("DriveRotate is not implemented.")));
            LogError("DriveRotate is not implemented.");
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_drivePort")]
		public void DriveSetPowerHandler(Drive.SetDrivePower setPower)
        {
			if (FaultIfNotConnected(setPower))
				return;

			if (!EnableCheck(setPower.ResponsePort, "SetPower")) return;

            if (Math.Abs(setPower.Body.LeftWheelPower) > 1.0f || Math.Abs(setPower.Body.RightWheelPower) > 1.0f)
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

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_drivePort")]
		public void DriveSetSpeedHandler(Drive.SetDriveSpeed setSpeed)
        {
			if (FaultIfNotConnected(setSpeed))
				return;

			if (!EnableCheck(setSpeed.ResponsePort, "SetSpeed")) return;

            RpEntity.SetVelocity((float)setSpeed.Body.LeftWheelSpeed, (float)setSpeed.Body.RightWheelSpeed);

            UpdateStateFromSimulation();
            setSpeed.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update notification for entire state
            _subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(_state.DriveState, DsspActions.UpdateRequest));
        }

		[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
		public void DriveEnableHandler(Drive.EnableDrive enable)
        {
			if (FaultIfNotConnected(enable))
				return;

            _state.DriveState.IsEnabled = enable.Body.Enable;
            RpEntity.IsEnabled = _state.DriveState.IsEnabled;

            UpdateStateFromSimulation();
            enable.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update for entire state
            _subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(_state.DriveState, DsspActions.UpdateRequest));
        }

		[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_drivePort")]
		public void DriveAllStopHandler(Drive.AllStop estop)
        {
			if (FaultIfNotConnected(estop))
				return;

            RpEntity.SetMotorTorque(0, 0);

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

		static Random _rnd = new Random();
        /// <summary>
        /// Updates the state from simulation.
        /// </summary>
        private void UpdateStateFromSimulation()
        {
	        _state.DriveState.TimeStamp = DateTime.Now;

	        // Reverse out the encoder ticks
	        _state.DriveState.LeftWheel.EncoderState.TimeStamp = _state.DriveState.TimeStamp;
	        _state.DriveState.LeftWheel.EncoderState.CurrentReading =
		        RotationsToTicks(RpEntity.LeftWheel) - _lastResetLeftWheelEncoderValue;
	        _state.DriveState.LeftWheel.EncoderState.CurrentReading += (int)Normal.Sample(_rnd, 0, _state.WheelTicksSigma.X);
	        _state.DriveState.RightWheel.EncoderState.CurrentReading =
		        RotationsToTicks(RpEntity.RightWheel) - _lastResetRightWheelEncoderValue;
			_state.DriveState.RightWheel.EncoderState.CurrentReading += (int)Normal.Sample(_rnd, 0, _state.WheelTicksSigma.Y);

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