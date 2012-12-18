using System;
using Microsoft.Dss.ServiceModel.Dssp;
using Brumba.Simulation.SimulatedAckermanFourWheels;
using SafwPx = Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation.SimulationTester
{
    public interface IServiceStarter
    {
        DsspResponsePort<CreateResponse> CreateService(ServiceInfoType serviceInfo);
        SafwPx.SimulatedAckermanFourWheelsOperations ServiceForwarder(Uri uri);
        void Activate(Choice choice);
    }

    public class AckermanFourWheelsCreator
    {
        public static DsspResponsePort<SafwPx.SimulatedAckermanFourWheelsOperations> CreateVehicleAndService(IServiceStarter starter, string name, Vector3 position, AckermanFourWheelsEntity.Builder builder)
        {
            var sav = new AckermanFourWheelsEntity(name, position, builder);
			builder.Build(sav);
            SimulationEngine.GlobalInstancePort.Insert(sav);

            return StartService(starter, name);
        }

        public static DsspResponsePort<SafwPx.SimulatedAckermanFourWheelsOperations> StartService(IServiceStarter starter, string name)
        {
            var res = new DsspResponsePort<SafwPx.SimulatedAckermanFourWheelsOperations>();
            starter.Activate(Arbiter.Choice(starter.CreateService(
                new ServiceInfoType
                {
                    Contract = SafwPx.Contract.Identifier,
                    PartnerList = { new PartnerType { Service = @"http://localhost/" + name, Name = new System.Xml.XmlQualifiedName("Entity", @"http://schemas.microsoft.com/robotics/2006/04/simulation.html") } }
                }),
                cr =>
                {
                    //LogInfo("SimulatedAckermanFourWheels service: " + cr.Service + " is created for entity " + name);
                    res.Post(starter.ServiceForwarder(new Uri(cr.Service)));
                },
                f =>
                {
                    //LogInfo("Could not create SimulatedAckermanFourWheels service for entity " + name);
                    res.Post(f);
                }));
            return res;
        }
    }
}
