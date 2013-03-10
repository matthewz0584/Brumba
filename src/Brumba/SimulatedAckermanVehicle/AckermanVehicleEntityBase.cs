using System;
using System.Linq;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation.SimulatedAckermanVehicle
{
    [DataContract]
    public class AckermanVehicleEntityBase : VisualEntity
    {
        protected float TargetAxleSpeed { get; set; }
        protected float TargetSteerAngle { get; set; }
        protected bool ToBreak { get; set; }

        protected float DriveAngularDistance { get; set; }
        protected float SteeringAngle { get; set; }

        [DataMember]
        public AckermanVehicleProperties Props { get; set; }

        public AckermanVehicleEntityBase()
            : this("", new Vector3(), new AckermanVehicleProperties()) {}

        public AckermanVehicleEntityBase(string name, Vector3 position, AckermanVehicleProperties props)
        {
            State.Name = name;
            State.Pose.Position = position;
            Props = props;
        }

        public void SetDrivePower(float power)
        {
            ToBreak = false;
            TargetAxleSpeed = power * MaxAxleSpeed;
        }

        public void SetSteeringAngle(float angle)
        {
            TargetSteerAngle = angle * Props.MaxSteeringAngle;
        }

        public float GetDriveAngularDistance()
        {
            return DriveAngularDistance;
        }

        public float GetSteeringAngle()
        {
            return SteeringAngle;
        }

        public void Break()
        {
            ToBreak = true;
        }

        public static float UpdateLinearValue(float targetValue, float currentValue, float delta)
        {
            return Math.Abs(targetValue - currentValue) > delta ? currentValue + Math.Sign(targetValue - currentValue) * delta : targetValue;
        }

        protected float MaxAxleSpeed
        {
            get { return Props.MaxVelocity / Props.WheelsProperties.First().Radius; }
        }
    }
}