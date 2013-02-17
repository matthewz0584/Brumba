using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Xna.Framework.Graphics;

namespace Brumba.Simulation.SimulatedAckermanVehicleEx
{
    [DataContract]
    public partial class AckermanVehicleExEntity : VisualEntity
    {
        private AckermanVehicleProperties _properties;
        private float _targetAxleSpeed;
        private float _targetSteerAngle;

        //[DataMember]
        public List<CompositeWheel> Wheels { get; set; }

        [DataMember]
        public List<BoxShape> ChassisParts { get; set; }

        [DataMember]
        public float MaxVelocity { get; set; }

        [DataMember]
        public float MaxSteerAngle { get; set; }

        /// <summary>
        /// Only for deserialization
        /// </summary>
        public AckermanVehicleExEntity()
        { 
        }

        public AckermanVehicleExEntity(string name, Vector3 position, AckermanVehicleProperties properties)
        {
            State.Name = name;
            State.Pose.Position = position;
            _properties = properties;
        }

        #region Overrides
        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            try
            {
                //New from simulator Entity\New menu (not deserialization)
                if (ChassisParts == null && _properties == null)
                    _properties = AckermanVehicles.HardRearDriven;

				if (_properties != null)
					new Builder(this, _properties).Build();

				Wheels = Children.OfType<CompositeWheel>().ToList();

                ChassisParts.ForEach(State.PhysicsPrimitives.Add);

                CreateAndInsertPhysicsEntity(physicsEngine);
                PhysicsEntity.SolverIterationCount = 64;

                base.Initialize(device, physicsEngine);
            }
            catch(Exception ex)
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

			base.Update(update);
        }

        #endregion

        public void SetMotorPower(float power)
        {
            _targetAxleSpeed = power * MaxAxleSpeed;
        }

        public void SetSteerAngle(float angle)
        {
            _targetSteerAngle = angle * MaxSteerAngle;
        }

        public void Break()
        {
            SetMotorPower(0);
            foreach (var w in Wheels.Where(w => w.Props.Motorized))
                w.AxleSpeed = 0;
        }

        public float Velocity
        {
            get { return Wheels.First(w => w.Props.Motorized).AxleSpeed * Wheels.First().Props.Radius; }
        }

        public float SteerAngle
        {
            get { return Wheels.First(w => w.Props.Steerable).SteerAngle; }
        }

        private void UpdateMotorAxleSpeed(float deltaT)
        {
            foreach (var w in Wheels.Where(w => w.Props.Motorized))
                w.AxleSpeed = UpdateLinearValue(_targetAxleSpeed, w.AxleSpeed, deltaT / 5 * MaxAxleSpeed);
        }

        private void UpdateSteerAngle(float deltaT)
        {
            foreach (var w in Wheels.Where(w => w.Props.Steerable))
                if (Math.Abs(w.SteerAngle - _targetSteerAngle) > 0.01f * Math.PI)
                    w.SteerAngle = UpdateLinearValue(_targetSteerAngle, w.SteerAngle, deltaT / 0.1f * MaxSteerAngle);
        }

        private static float UpdateLinearValue(float targetValue, float currentValue, float delta)
        {
            return Math.Abs(targetValue - currentValue) > delta ? currentValue + Math.Sign(targetValue - currentValue) * delta : targetValue;
        }

        private float MaxAxleSpeed
        {
            get { return MaxVelocity / Wheels.First().Props.Radius; }
        }
    }
}


