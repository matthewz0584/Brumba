using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using Microsoft.Robotics.Simulation;

namespace Brumba.Simulation.SimulatedAckermanFourWheels
{
    [DataContract]
    public partial class AckermanFourWheelsEntity : VisualEntity
    {
        private Builder _builder;
        private float _targetAxleSpeed;
        private float _targetSteerAngle;

        [DataMember]
        [Category("Wheels")]
        public CompositeWheel WheelFl { get; set; }

        [DataMember]
        [Category("Wheels")]
        public CompositeWheel WheelFr { get; set; }

        [DataMember]
        [Category("Wheels")]
        public CompositeWheel WheelRl { get; set; }

        [DataMember]
        [Category("Wheels")]
        public CompositeWheel WheelRr { get; set; }

        [DataMember]
        public List<BoxShape> ChassisParts { get; set; }

        [DataMember]
        public float MaxVelocity { get; set; }

        [DataMember]
        public float MaxSteerAngle { get; set; }

        public double ElapsedTime { get; private set; }

        /// <summary>
        /// Only for deserialization
        /// </summary>
        public AckermanFourWheelsEntity()
        { 
        }

        public AckermanFourWheelsEntity(string name, Vector3 position, Builder builder)
        {
            State.Name = name;
            State.Pose.Position = position;
            _builder = builder;
        }

        #region Overrides
        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            try
            {
                //New from simulator Entity\New (not deserialization)
                if (ChassisParts == null)
                    _builder = Builder.Default;

                if (_builder != null)
                    _builder.Build(this);

                ChassisParts.ForEach(State.PhysicsPrimitives.Add);

                CreateAndInsertPhysicsEntity(physicsEngine);
                PhysicsEntity.SolverIterationCount = 64;

                base.Initialize(device, physicsEngine);

                Wheels.ForEach(w => w.Initialize(this, device, physicsEngine));
            }
            catch(Exception ex)
            {
                if (PhysicsEntity != null)
                    PhysicsEngine.DeleteEntity(PhysicsEntity);
                HasBeenInitialized = false;
                InitError = ex.ToString();
            }
        }

        public override void Render(VisualEntity.RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
            Wheels.ForEach(w => w.Render(renderMode, transforms, currentCamera));
            base.Render(renderMode, transforms, currentCamera);
        }

        public override void Update(FrameUpdate update)
        {
            base.Update(update);

            UpdateMotorAxleSpeed((float)update.ElapsedTime);
            UpdateSteerAngle((float)update.ElapsedTime);

            Wheels.ForEach(w => w.Update(update));

            ElapsedTime = update.ApplicationTime;
        }

        public override void Dispose()
        {
            Wheels.ForEach(w => w.Dispose());
            base.Dispose();
        }

        #endregion

        public void SetMotorPower(float power)
        {
            _targetAxleSpeed = power * MaxAxleSpeed;
        }

        public void SetSteerAngle(float angle)
        {
            _targetSteerAngle = -angle * MaxSteerAngle;
        }

        public void Break()
        {
            SetMotorPower(0);
            WheelRr.AxleSpeed = WheelRl.AxleSpeed = 0;
        }

        private void UpdateMotorAxleSpeed(float deltaT)
        {
            WheelRr.AxleSpeed = WheelRl.AxleSpeed = UpdateLinearValue(_targetAxleSpeed, WheelRl.AxleSpeed, deltaT / 5 * MaxAxleSpeed);
        }

        private void UpdateSteerAngle(float deltaT)
        {
            WheelFr.SteerAngle = WheelFl.SteerAngle = UpdateLinearValue(_targetSteerAngle, WheelFl.SteerAngle, deltaT / 0.1f * MaxSteerAngle);
        }

        private static float UpdateLinearValue(float targetValue, float currentValue, float delta)
        {
            return Math.Abs(targetValue - currentValue) > delta ? currentValue + Math.Sign(targetValue - currentValue) * delta : targetValue;
        }

        private float MaxAxleSpeed
        {
            get { return MaxVelocity / WheelRl.Radius; }
        }

        private List<CompositeWheel> Wheels
        {
            get { return new List<CompositeWheel> { WheelFl, WheelFr, WheelRl, WheelRr }; }
        }
    }
}


