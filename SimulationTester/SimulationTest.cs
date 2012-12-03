using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using SimPxy = Microsoft.Robotics.Simulation.Proxy;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Brumba.Simulation.SimulatedAckermanFourWheels;
using SafwPxy = Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Diagnostics;
using Xna = Microsoft.Xna.Framework;
using System.Globalization;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.Simulation;
using Microsoft.Dss.Services.Serializer;
using Microsoft.Dss.Core;
using Microsoft.Dss.Services.MountService;
using MountPxy = Microsoft.Dss.Services.MountService;

namespace Brumba.Simulation.SimulationTester
{
    abstract class Test
    {
        public double EstimatedTime { get; protected set; }

        public abstract List<string> ObjectsToRestore();

        public IEnumerator<ITask> Start(SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            yield return To.Exec(ConcreteStart, (double et) => EstimatedTime = et, vehiclePort);
        }

        public abstract IEnumerator<ITask> ConcreteStart(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort);

        public abstract IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime);
    }

    class Test1 : Test
    {
        public override List<string> ObjectsToRestore()
        {
            return new List<string> { "testee" };
        }

        public override IEnumerator<ITask> ConcreteStart(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            float motorPower = 0.6f;
            yield return To.Exec(vehiclePort.SetMotorPower(new SafwPxy.MotorPowerRequest { Value = motorPower }));
            @return(50 / (AckermanFourWheelsEntity.Builder.Default.MaxVelocity * motorPower));//50 meters
        }

        public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
        {
            var vehEntity = simStateEntities.Where(e => e.State.Name == "testee").Single();
            var pos = TypeConversion.ToXNA((Vector3)DssTypeHelper.TransformFromProxy(vehEntity.State.Pose.Position));
            @return(pos.Length() > 50);
            yield break;
        }
    }

    class Test2 : Test
    {
        public override List<string> ObjectsToRestore()
        {
            return new List<string> { "testee" };
        }

        public override IEnumerator<ITask> ConcreteStart(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            float motorPower = 0.2f;
            float steerAngle = 0.5f;
            yield return To.Exec(vehiclePort.SetSteerAngle(new SafwPxy.SteerAngleRequest { Value = steerAngle }));
            yield return To.Exec(vehiclePort.SetMotorPower(new SafwPxy.MotorPowerRequest { Value = motorPower }));
            @return(10);
        }

        public override IEnumerator<ITask> AssessProgress(Action<bool> @return, IEnumerable<EngPxy.VisualEntity> simStateEntities, double elapsedTime)
        {
            var vehEntity = simStateEntities.Where(e => e.State.Name == "testee").Single();
            var orientation = UIMath.QuaternionToEuler((Quaternion)DssTypeHelper.TransformFromProxy(vehEntity.State.Pose.Orientation));
            @return(Math.Abs(orientation.X) < 90 && elapsedTime > EstimatedTime);
            yield break;
        }
    }
}


