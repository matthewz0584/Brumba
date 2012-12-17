﻿using System;
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
using Brumba.Simulation.SimulationTester;

namespace Brumba.Simulation.SimpleAckermanVehicle
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
            GenerateEnvironment();
            //GenerateEnvironmentForTests();

            base.Start();
        }

        private void GenerateEnvironment()
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

            Activate(Arbiter.Choice(AckermanFourWheelsCreator.CreateVehicleAndService(this, "testee", new Vector3(0, 0.2f, 0), AckermanFourWheelsEntity.Builder.Simple),
                ops4 =>
                {
                    //ops4.SetMotorPower(new SafwProxy.MotorPowerRequest { Value = 0.2f });
                    ops4.SetSteerAngle(new SafwProxy.SteerAngleRequest { Value = -0.25f });
                },
                f => LogInfo("bebebe")));
        }

        private void GenerateEnvironmentForTests()
        {
            var terrain = new TerrainEntity(@"terrain_file.bmp", "terrain_tex.jpg", new MaterialProperties("ground", 0, 0.5f, 1.0f))
            {
                State = { Name = "Terrain", Assets = { Effect = "Terrain.fx" } },
            };
            SimulationEngine.GlobalInstancePort.Insert(terrain);

            var view = new CameraView { EyePosition = new Vector3(-12f, 9f, -3f), LookAtPoint = new Vector3(0.36f, 0.49f, 0.06f) };
            SimulationEngine.GlobalInstancePort.Update(view);

            SkyDomeEntity sky = new SkyDomeEntity("skydome.dds", "sky_diff.dds");
            SimulationEngine.GlobalInstancePort.Insert(sky);

            var sun = new LightSourceEntity
            {
                State = { Name = "Sun" },
                Type = LightSourceEntityType.Directional,
                Color = new Vector4(0.8f, 0.8f, 0.8f, 1),
                Direction = new Vector3(0.5f, -.75f, 0.5f)
            };
            SimulationEngine.GlobalInstancePort.Insert(sun);

            Activate(Arbiter.Choice(AckermanFourWheelsCreator.CreateVehicleAndService(this, "testee", new Vector3(), AckermanFourWheelsEntity.Builder.Simple4x4),
                ops4 => {}, f => LogInfo("bebebe")));
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
