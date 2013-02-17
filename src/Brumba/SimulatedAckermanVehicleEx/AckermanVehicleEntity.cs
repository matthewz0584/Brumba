using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework.Graphics;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    [DataContract]
    public partial class AckermanVehicleEntity : VisualEntity
    {
        float _targetAxleSpeed;
        float _targetSteerAngle;

        [DataMember]
        public List<WheelEntity> Wheels { get; set; }

        [DataMember]
        public BoxShape Chassis { get; set; }

        [DataMember]
        public AckermanVehicleProperties Props { get; set; }

        /// <summary>
        /// Only for deserialization
        /// </summary>
        public AckermanVehicleEntity()
        {
        }

        public AckermanVehicleEntity(string name, Vector3 position, AckermanVehicleProperties properties)
        {
            State.Name = name;
            State.Pose.Position = position;
            Props = properties;
        }

        #region Overrides
        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            try
            {
                //New from simulator Entity\New menu (not deserialization)
                if (Wheels == null && Chassis == null && Props == null)
                    Props = AckermanVehicles.Simplistic;

                if (Wheels == null && Chassis == null && Props != null)
                    new Builder(this, Props).Build();

                State.PhysicsPrimitives.Add(Chassis);

                CreateAndInsertPhysicsEntity(physicsEngine);
                PhysicsEntity.SolverIterationCount = 16;

                Wheels.ForEach(w =>
                {
                    w.Parent = this;
                    w.Initialize(device, physicsEngine);
                });

                base.Initialize(device, physicsEngine);
            }
            catch (Exception ex)
            {
                if (PhysicsEntity != null)
                    PhysicsEngine.DeleteEntity(PhysicsEntity);
                HasBeenInitialized = false;
                InitError = ex.ToString();
            }
        }

        public override void Update(FrameUpdate update)
        {
            UpdateMotorAxleSpeed((float)update.ElapsedTime);
            UpdateSteerAngle((float)update.ElapsedTime);

            Wheels.ForEach(w => w.Update(update));
            base.Update(update);
        }

        public override void Render(RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
            Wheels.ForEach(w => w.Render(renderMode, transforms, currentCamera));
            base.Render(renderMode, transforms, currentCamera);
        }

        public override void Dispose()
        {
            Wheels.ForEach(w => w.Dispose());
            base.Dispose();
        }

        #endregion

        public void SetDrivePower(float power)
        {
            _targetAxleSpeed = power * MaxAxleSpeed;
        }

        public void SetSteerAngle(float angle)
        {
            _targetSteerAngle = angle * Props.MaxSteerAngle;
        }

        public void Break()
        {
            SetDrivePower(0);
            foreach (var w in Wheels)
                w.Wheel.AxleSpeed = 0;
        }

        void UpdateMotorAxleSpeed(float deltaT)
        {
            foreach (var w in Wheels)
                w.Wheel.AxleSpeed = UpdateLinearValue(_targetAxleSpeed, w.Wheel.AxleSpeed, deltaT / 5 * MaxAxleSpeed);
        }

        void UpdateSteerAngle(float deltaT)
        {
            foreach (var w in Wheels.Where((w, i) => Props.WheelsProperties.ToList()[i].Steerable))
                if (Math.Abs(w.Wheel.SteerAngle - _targetSteerAngle) > 0.01f * Math.PI)
                    w.Wheel.SteerAngle = UpdateLinearValue(_targetSteerAngle, w.Wheel.SteerAngle, deltaT / 0.1f * Props.MaxSteerAngle);
        }

        static float UpdateLinearValue(float targetValue, float currentValue, float delta)
        {
            return Math.Abs(targetValue - currentValue) > delta ? currentValue + Math.Sign(targetValue - currentValue) * delta : targetValue;
        }

        float MaxAxleSpeed
        {
            get { return Props.MaxVelocity / Props.WheelsProperties.First().Radius; }
        }
    }
}