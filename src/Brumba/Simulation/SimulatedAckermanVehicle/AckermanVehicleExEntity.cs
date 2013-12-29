using System;
using System.Linq;
using System.Collections.Generic;
using Brumba.Simulation.SimulatedTimer;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Xna.Framework.Graphics;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    [DataContract]
    public partial class AckermanVehicleExEntity : AckermanVehicleEntityBase
    {
        public List<CompositeWheel> Wheels { get; private set; }

        [DataMember]
        public List<BoxShape> ChassisParts { get; set; }

        /// <summary>
        /// Only for deserialization
        /// </summary>
        public AckermanVehicleExEntity() {}

        public AckermanVehicleExEntity(string name, Vector3 position, AckermanVehicleProperties props)
            : base(name, position, props) {}

        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            try
            {
                //New from simulator Entity\New menu (not deserialization)
                if (ChassisParts == null && Props == null)
                    Props = AckermanVehicles.HardRearDriven;

                if (ChassisParts == null && Props != null)
					new Builder(this, Props).Build();

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
            UpdateState(SimulatedTimerService.GetElapsedTime(update));
            UpdateDriveAxleSpeed(SimulatedTimerService.GetElapsedTime(update));
            UpdateSteerAngle(SimulatedTimerService.GetElapsedTime(update));

			base.Update(update);
        }

        void UpdateState(float deltaT)
        {
            SteeringAngle = Wheels.First(w => w.Props.Steerable).SteeringAngle;
            DriveAngularDistance += Wheels.First(w => w.Props.Motorized).AxleSpeed * deltaT;
        }

        void UpdateDriveAxleSpeed(float deltaT)
        {
            if (ToBreak)
            {
                SetDrivePower(0);
                foreach (var w in Wheels.Where(w => w.Props.Motorized))
                    w.AxleSpeed = 0;
                return;
            }

            foreach (var w in Wheels.Where(w => w.Props.Motorized))
                w.AxleSpeed = UpdateLinearValue(TargetAxleSpeed, w.AxleSpeed, deltaT / 5 * MaxAxleSpeed);
        }

        void UpdateSteerAngle(float deltaT)
        {
            foreach (var w in Wheels.Where(w => w.Props.Steerable))
                if (Math.Abs(w.SteeringAngle - TargetSteerAngle) > 0.01f * Math.PI)
                    w.SteeringAngle = UpdateLinearValue(TargetSteerAngle, w.SteeringAngle, deltaT / 0.01f * Props.MaxSteeringAngle);
        }
    }
}


