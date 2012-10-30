using System;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;
using Brumba.Simulation.SimulatedAckermanFourWheels;
using SafwProxy = Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using System.Threading;
using Microsoft.Ccr.Core.Arbiters;

namespace Brumba.Simulation
{
    [Contract(Contract.Identifier)]
    [DisplayName("SimpleAckermanVehicle")]
    [Description("SimpleAckermanVehicle service (no description provided)")]
    class SimpleAckermanVehicleService : DsspServiceBase, IServiceStarter
    {
        [ServiceState]
        SimpleAckermanVehicleState _state = new SimpleAckermanVehicleState();

        [ServicePort("/SimpleAckermanVehicle", AllowMultipleInstances = true)]
        SimpleAckermanVehicleOperations _mainPort = new SimpleAckermanVehicleOperations();

        [Partner("Engine", Contract = Microsoft.Robotics.Simulation.Engine.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        private SimulationEnginePort _engineStub = new SimulationEnginePort();//only for auto engine creation

        public SimpleAckermanVehicleService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        protected override void Start()
        {
            var view = new CameraView { EyePosition = new Vector3(-1.65f, 1.63f, -0.29f), LookAtPoint = new Vector3(0, 0, 0) };
            SimulationEngine.GlobalInstancePort.Update(view);

            SkyDomeEntity sky = new SkyDomeEntity("skydome.dds", "sky_diff.dds");
            SimulationEngine.GlobalInstancePort.Insert(sky);

            var sun = new LightSourceEntity() { Type = LightSourceEntityType.Directional, Color = new Vector4(0.8f, 0.8f, 0.8f, 1), Direction = new Vector3(0.5f, -.75f, 0.5f) };
            sun.State.Name = "Sun";
            SimulationEngine.GlobalInstancePort.Insert(sun);

            var ground = new HeightFieldEntity("Ground", "WoodFloor.dds", new MaterialProperties("ground", 0f, 0.5f, 0.5f));
            SimulationEngine.GlobalInstancePort.Insert(ground);
            //var ground = new TerrainEntity("terrain.bmp", "", new MaterialProperties("ground", 0f, 0.5f, 0.5f));
            //SimulationEngine.GlobalInstancePort.Insert(ground);

            var box = new BoxShape(new BoxShapeProperties(10, new Pose(), new Vector3(1, 0.03f, 0.5f)) { Material = new MaterialProperties("ground", 0f, 0.5f, 0.5f) });
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(box, new Vector3(0, 0.02f, 2f)) { State = { Name = "booox" } });

            //var cme = new TriangleMeshEnvironmentEntity(new Vector3(0, 0.3f, 0.5f), "WheelShape2.obj", null)
            //{ 
            //    State = { Name = "wheeel", MassDensity = { Mass = 2 } },
            //    Material = new MaterialProperties("tire", 1f, 0.9f, 2.0f)
            //};
            //SimulationEngine.GlobalInstancePort.Insert(cme);


            Activate(Arbiter.Choice(AckermanFourWheelsCreator.Insert(this, "SAV2", new Vector3(0, 0.2f, 0), AckermanFourWheelsEntity.Builder.Default),
                ops4 =>
                {
                    //ops4.SetMotorPower(new SafwProxy.MotorPowerRequest { Value = 0.2f });
                    ops4.SetSteerAngle(new SafwProxy.SteerAngleRequest { Value = -0.25f });
                },
                f => LogInfo("bebebe")));

            base.Start();
        }

        #region IServiceCreator
        DsspResponsePort<CreateResponse> IServiceStarter.CreateService(ServiceInfoType serviceInfo)
        {
            return CreateService(serviceInfo);
        }

        SafwProxy.SimulatedAckermanFourWheelsOperations IServiceStarter.ServiceForwarder(Uri uri)
        {
            return ServiceForwarder<SafwProxy.SimulatedAckermanFourWheelsOperations>(uri);
        }

        void IServiceStarter.Activate(Choice choice)
        {
            Activate(choice);
        }
        #endregion
    }
}
