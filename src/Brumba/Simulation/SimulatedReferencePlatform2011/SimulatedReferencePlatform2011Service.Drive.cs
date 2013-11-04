//------------------------------------------------------------------------------
//  <copyright file="SimulatedDrive.cs" company="Microsoft Corporation">
//      Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Robotics.Simulation.Engine;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedReferencePlatform2011
{
    partial class SimulatedReferencePlatform2011Service
    {
        /// <summary>
        /// The constant string used for specifying the alternate service port 
        /// </summary>
        private const string DrivePortName = "_drivePort";

        /// <summary>
        /// The subscription manager
        /// </summary>
        [SubscriptionManagerPartner("SubMgr")]
        private Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort subMgrPort = new Microsoft.Dss.Services.SubscriptionManager.SubscriptionManagerPort();

        /// <summary>
        /// The drive operation port
        /// </summary>
        [AlternateServicePort("/DifferentialDrive", AllowMultipleInstances = true,
            AlternateContract = Microsoft.Robotics.Services.Drive.Contract.Identifier)]
        private Microsoft.Robotics.Services.Drive.DriveOperations _drivePort = new Microsoft.Robotics.Services.Drive.DriveOperations();

        /// <summary>
        /// The encoder value of the left wheel at last reset
        /// </summary>
        private int lastResetLeftWheelEncoderValue;

        /// <summary>
        /// The encoder value of the right wheel at last reset
        /// </summary>
        private int lastResetRightWheelEncoderValue;

        /// <summary>
        /// Get handler retrieves service state
        /// </summary>
        /// <param name="get">The Get request</param>
        //[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = DrivePortName)]
        public void DriveHttpGetHandler(HttpGet get)
        {
            this.UpdateStateFromSimulation();
            get.ResponsePort.Post(new HttpResponseType(this._state.DriveState));
        }

        /// <summary>
        /// Get handler retrieves service state
        /// </summary>
        /// <param name="get">The Get request</param>
        //[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = DrivePortName)]
        public void DriveGetHandler(Microsoft.Robotics.Services.Drive.Get get)
        {
            this.UpdateStateFromSimulation();
            get.ResponsePort.Post(this._state.DriveState);
        }

        #region Subscribe Handling
        /// <summary>
        /// Subscribe to Differential Drive service
        /// </summary>
        /// <param name="subscribe">The subscribe request</param>
        /// <returns>Standard ccr iterator.</returns>
        //[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = DrivePortName)]
        public IEnumerator<ITask> DriveSubscribeHandler(Microsoft.Robotics.Services.Drive.Subscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(this.subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
                this.subMgrPort.Post(
                    new Microsoft.Dss.Services.SubscriptionManager.Submit(subscribe.Body.Subscriber, DsspActions.UpdateRequest, this._state.DriveState, null)),
                LogError);
        }

        /// <summary>
        /// Subscribe to Differential Drive service
        /// </summary>
        /// <param name="subscribe">The subscribe request</param>
        /// <returns>Standard ccr iterator.</returns>
        //[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = DrivePortName)]
        public IEnumerator<ITask> DriveReliableSubscribeHandler(Microsoft.Robotics.Services.Drive.ReliableSubscribe subscribe)
        {
            yield return Arbiter.Choice(
                SubscribeHelper(this.subMgrPort, subscribe.Body, subscribe.ResponsePort),
                success =>
                this.subMgrPort.Post(
                    new Microsoft.Dss.Services.SubscriptionManager.Submit(subscribe.Body.Subscriber, DsspActions.UpdateRequest, this._state.DriveState, null)),
                LogError);
        }
        #endregion

        /// <summary>
        /// ResetsEncoders handler.
        /// </summary>
        /// <param name="resetEncoders">The reset encoders request</param>
        //[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = DrivePortName)]
        public void ResetEncodersHandler(Microsoft.Robotics.Services.Drive.ResetEncoders resetEncoders)
        {
            this.lastResetLeftWheelEncoderValue += this._state.DriveState.LeftWheel.EncoderState.CurrentReading;
            this.lastResetRightWheelEncoderValue += this._state.DriveState.RightWheel.EncoderState.CurrentReading;
            resetEncoders.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        /// <summary>
        /// Handler for drive request
        /// </summary>
        /// <param name="driveDistance">The DriveDistance request</param>
        //[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = DrivePortName)]
        public void DriveDistanceHandler(Microsoft.Robotics.Services.Drive.DriveDistance driveDistance)
        {
            if (this.RpEntity == null)
            {
                throw new InvalidOperationException("Simulation entity not registered with service");
            }

            if (!this._state.DriveState.IsEnabled)
            {
                driveDistance.ResponsePort.Post(Fault.FromException(new Exception("Drive is not enabled.")));
                LogError("DriveDistance request to disabled drive.");
                return;
            }

            if ((driveDistance.Body.Power > 1.0f) || (driveDistance.Body.Power < -1.0f))
            {
                // invalid drive power
                driveDistance.ResponsePort.Post(Fault.FromException(new Exception("Invalid Power parameter.")));
                LogError("Invalid Power parameter in DriveDistanceHandler."); 
                return;
            }

            this._state.DriveState.DriveDistanceStage = driveDistance.Body.DriveDistanceStage;
            if (driveDistance.Body.DriveDistanceStage == Microsoft.Robotics.Services.Drive.DriveStage.InitialRequest)
            {
                var entityResponse = new Port<OperationResult>();
                Activate(
                    Arbiter.Receive(
                        false,
                        entityResponse,
                        result =>
                            {
                                // post a message to ourselves indicating that the drive distance has completed
                                var req = new Microsoft.Robotics.Services.Drive.DriveDistanceRequest(0, 0);
                                switch (result)
                                {
                                    case Microsoft.Robotics.Simulation.Engine.OperationResult.Error:
                                        req.DriveDistanceStage = Microsoft.Robotics.Services.Drive.DriveStage.Canceled;
                                        break;
                                    case Microsoft.Robotics.Simulation.Engine.OperationResult.Canceled:
                                        req.DriveDistanceStage = Microsoft.Robotics.Services.Drive.DriveStage.Canceled;
                                        break;
                                    case Microsoft.Robotics.Simulation.Engine.OperationResult.Completed:
                                        req.DriveDistanceStage = Microsoft.Robotics.Services.Drive.DriveStage.Completed;
                                        break;
                                }

                                this._drivePort.Post(new Microsoft.Robotics.Services.Drive.DriveDistance(req));
                            }));

                this.RpEntity.DriveDistance(
                    (float)driveDistance.Body.Distance, (float)driveDistance.Body.Power, entityResponse);

                var req2 = new Microsoft.Robotics.Services.Drive.DriveDistanceRequest(0, 0)
                    { DriveDistanceStage = Microsoft.Robotics.Services.Drive.DriveStage.Started };
                this._drivePort.Post(new Microsoft.Robotics.Services.Drive.DriveDistance(req2));
            }
            else
            {
                SendNotification(this.subMgrPort, driveDistance);
            }

            driveDistance.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        /// <summary>
        /// Handler for rotate request
        /// </summary>
        /// <param name="rotate">The RotateDegrees request</param>
        //[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = DrivePortName)]
        public void DriveRotateHandler(Microsoft.Robotics.Services.Drive.RotateDegrees rotate)
        {
            if (this.RpEntity == null)
            {
                throw new InvalidOperationException("Simulation entity not registered with service");
            }

            if (!this._state.DriveState.IsEnabled)
            {
                rotate.ResponsePort.Post(Fault.FromException(new Exception("Drive is not enabled.")));
                LogError("RotateDegrees request to disabled drive.");
                return;
            }

            this._state.DriveState.RotateDegreesStage = rotate.Body.RotateDegreesStage;
            if (rotate.Body.RotateDegreesStage == Microsoft.Robotics.Services.Drive.DriveStage.InitialRequest)
            {
                var entityResponse = new Port<OperationResult>();
                Activate(
                    Arbiter.Receive(
                        false,
                        entityResponse,
                        result =>
                            {
                                // post a message to ourselves indicating that the drive distance has completed
                                var req = new Microsoft.Robotics.Services.Drive.RotateDegreesRequest(0, 0);
                                switch (result)
                                {
                                    case Microsoft.Robotics.Simulation.Engine.OperationResult.Error:
                                        req.RotateDegreesStage = Microsoft.Robotics.Services.Drive.DriveStage.Canceled;
                                        break;
                                    case Microsoft.Robotics.Simulation.Engine.OperationResult.Canceled:
                                        req.RotateDegreesStage = Microsoft.Robotics.Services.Drive.DriveStage.Canceled;
                                        break;
                                    case Microsoft.Robotics.Simulation.Engine.OperationResult.Completed:
                                        req.RotateDegreesStage = Microsoft.Robotics.Services.Drive.DriveStage.Completed;
                                        break;
                                }

                                this._drivePort.Post(new Microsoft.Robotics.Services.Drive.RotateDegrees(req));
                            }));

                this.RpEntity.RotateDegrees((float)rotate.Body.Degrees, (float)rotate.Body.Power, entityResponse);

                var req2 = new Microsoft.Robotics.Services.Drive.RotateDegreesRequest(0, 0)
                    { RotateDegreesStage = Microsoft.Robotics.Services.Drive.DriveStage.Started };
                this._drivePort.Post(new Microsoft.Robotics.Services.Drive.RotateDegrees(req2));
            }
            else
            {
                SendNotification(this.subMgrPort, rotate);
            }

            rotate.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

        /// <summary>
        /// Handler for setting the drive power
        /// </summary>
        /// <param name="setPower">The SetDrivePower request</param>
        //[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = DrivePortName)]
        public void DriveSetPowerHandler(Microsoft.Robotics.Services.Drive.SetDrivePower setPower)
        {
            if (this.RpEntity == null)
            {
                throw new InvalidOperationException("Simulation entity not registered with service");
            }

            if (!this._state.DriveState.IsEnabled)
            {
                setPower.ResponsePort.Post(Fault.FromException(new Exception("Drive is not enabled.")));
                LogError("SetPower request to disabled drive.");
                return;
            }

            if ((setPower.Body.LeftWheelPower > 1.0f) || (setPower.Body.LeftWheelPower < -1.0f) ||
                (setPower.Body.RightWheelPower > 1.0f) || (setPower.Body.RightWheelPower < -1.0f))
            {
                // invalid drive power
                setPower.ResponsePort.Post(Fault.FromException(new Exception("Invalid Power parameter.")));
                LogError("Invalid Power parameter in SetPowerHandler.");
                return;
            }

            // Call simulation entity method for setting wheel torque
            this.RpEntity.SetMotorTorque((float)setPower.Body.LeftWheelPower, (float)setPower.Body.RightWheelPower);

            this.UpdateStateFromSimulation();
            setPower.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update notification for entire state
            this.subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(this._state.DriveState, DsspActions.UpdateRequest));
        }

        /// <summary>
        /// Handler for setting the drive speed
        /// </summary>
        /// <param name="setSpeed">The SetSpeed request</param>
        //[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = DrivePortName)]
        public void DriveSetSpeedHandler(Microsoft.Robotics.Services.Drive.SetDriveSpeed setSpeed)
        {
            if (this.RpEntity == null)
            {
                throw new InvalidOperationException("Simulation entity not registered with service");
            }

            if (!this._state.DriveState.IsEnabled)
            {
                setSpeed.ResponsePort.Post(Fault.FromException(new Exception("Drive is not enabled.")));
                LogError("SetSpeed request to disabled drive.");
                return;
            }

            this.RpEntity.SetVelocity((float)setSpeed.Body.LeftWheelSpeed, (float)setSpeed.Body.RightWheelSpeed);

            this.UpdateStateFromSimulation();
            setSpeed.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update notification for entire state
            this.subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(this._state.DriveState, DsspActions.UpdateRequest));
        }

        /// <summary>
        /// Handler for enabling or disabling the drive
        /// </summary>
        /// <param name="enable">The enable message</param>
        //[ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = DrivePortName)]
        public void DriveEnableHandler(Microsoft.Robotics.Services.Drive.EnableDrive enable)
        {
            if (this.RpEntity == null)
            {
                throw new InvalidOperationException("Simulation entity not registered with service");
            }

            this._state.DriveState.IsEnabled = enable.Body.Enable;
            this.RpEntity.IsEnabled = this._state.DriveState.IsEnabled;

            this.UpdateStateFromSimulation();
            enable.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update for entire state
            this.subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(this._state.DriveState, DsspActions.UpdateRequest));
        }

        /// <summary>
        /// Handler when the drive receives an all stop message
        /// </summary>
        /// <param name="estop">The stop message</param>
        //[ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = DrivePortName)]
        public void DriveAllStopHandler(Microsoft.Robotics.Services.Drive.AllStop estop)
        {
            if (this.RpEntity == null)
            {
                throw new InvalidOperationException("Simulation entity not registered with service");
            }

            this.RpEntity.SetMotorTorque(0, 0);
            this.RpEntity.SetVelocity(0);

            // AllStop disables the drive
            this.RpEntity.IsEnabled = false;

            this.UpdateStateFromSimulation();
            estop.ResponsePort.Post(DefaultUpdateResponseType.Instance);

            // send update for entire state
            this.subMgrPort.Post(new Microsoft.Dss.Services.SubscriptionManager.Submit(this._state.DriveState, DsspActions.UpdateRequest));
        }

        /// <summary>
        /// Start initializes service state and listens for drop messages
        /// </summary>
        protected void StartSimDrive()
        {
            if (this._state.DriveState == null)
            {
                this.CreateDefaultDriveState();
            }

            // enabled by default
            this._state.DriveState.IsEnabled = true;
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
        /// Creates the default state of the drive.
        /// </summary>
        private void CreateDefaultDriveState()
        {
            this._state.DriveState = new Microsoft.Robotics.Services.Drive.DriveDifferentialTwoWheelState
                {
                    LeftWheel = new Microsoft.Robotics.Services.Motor.WheeledMotorState 
                    { 
                        MotorState = new Microsoft.Robotics.Services.Motor.MotorState(),
                        EncoderState = new Microsoft.Robotics.Services.Encoder.EncoderState() 
                    },
                    RightWheel = new Microsoft.Robotics.Services.Motor.WheeledMotorState
                    {
                        MotorState = new Microsoft.Robotics.Services.Motor.MotorState(),
                        EncoderState = new Microsoft.Robotics.Services.Encoder.EncoderState()
                    },
                };
        }

        /// <summary>
        /// Updates the state from simulation.
        /// </summary>
        private void UpdateStateFromSimulation()
        {
            if (this.RpEntity != null)
            {
                this._state.DriveState.TimeStamp = DateTime.Now;

                // Reverse out the encoder ticks
                this._state.DriveState.LeftWheel.EncoderState.TimeStamp = this._state.DriveState.TimeStamp;
                this._state.DriveState.LeftWheel.EncoderState.CurrentReading =
                    RotationsToTicks(this.RpEntity.LeftWheel) - this.lastResetLeftWheelEncoderValue;
                this._state.DriveState.RightWheel.EncoderState.CurrentReading =
                    RotationsToTicks(this.RpEntity.RightWheel) - this.lastResetRightWheelEncoderValue;

                // Compute the wheel speeds
                this._state.DriveState.LeftWheel.WheelSpeed = -this.RpEntity.LeftWheel.Wheel.AxleSpeed
                                                             * this.RpEntity.LeftWheel.Wheel.State.Radius;
                this._state.DriveState.RightWheel.WheelSpeed = -this.RpEntity.RightWheel.Wheel.AxleSpeed
                                                             * this.RpEntity.RightWheel.Wheel.State.Radius;

                // Compute the power
                this._state.DriveState.LeftWheel.MotorState.CurrentPower = this.RpEntity.LeftWheel.Wheel.MotorTorque;
                this._state.DriveState.RightWheel.MotorState.CurrentPower = this.RpEntity.RightWheel.Wheel.MotorTorque;
                this._state.DriveState.IsEnabled = this.RpEntity.IsEnabled;
            }
        }
    }
}