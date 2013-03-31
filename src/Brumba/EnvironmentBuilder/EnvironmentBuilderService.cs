﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Brumba.Simulation.SimulatedAckermanVehicle;
using Brumba.Simulation.SimulatedTail;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;

namespace Brumba.Simulation.EnvironmentBuilder
{
    [Contract(Contract.Identifier)]
    [DisplayName("EnvironmentBuilder")]
    [Description("EnvironmentBuilder service (no description provided)")]
    class EnvironmentBuilderService : DsspServiceBase
    {
        [ServiceState]
        EnvironmentBuilderState _state = new EnvironmentBuilderState();

        [ServicePort("/EnvironmentBuilder", AllowMultipleInstances = true)]
        EnvironmentBuilderOperations _mainPort = new EnvironmentBuilderOperations();

        [Partner("Engine", Contract = Microsoft.Robotics.Simulation.Engine.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        private SimulationEnginePort _engineStub = new SimulationEnginePort();//only for auto engine creation

        public EnvironmentBuilderService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        protected override void Start()
        {
			//CrossCountryGenerator.Generate(257, 0.1f).Save("terrain00.bmp");
			//PopulateStabilizer();
			//PopulateAckermanVehicle();
			//GenerateTest();
			//PopulateAckermanVehicleWithTail();
	        //PopulateEnvForGroundTail();
            //PopulatePuckRobot();
            PopulateInfraredRfRing();

            base.Start();
        }

        void PopulateInfraredRfRing()
        {
            PopulateSimpleEnvironment();

            var ring = new InfraredRfRingEntity("rf ring", new Pose(new Vector3(0, 0, -0.3f)),
                                                new InfraredRfProperties
                                                    {
                                                        DispersionConeAngle = 4f,
                                                        Samples = 3f,
                                                        MaximumRange = 1,
                                                        ScanInterval = 0.025f
                                                    })
                {
                    RfPositionsPolar =
                            {
                                new Vector2(0, 0.1f),
                                new Vector2((float) Math.PI/2, 0.2f),
                                new Vector2((float) Math.PI, 0.3f)
                            }
                };

            var box = new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.3f)) { Material = new MaterialProperties("qq", 0.2f, 0.8f, 1.0f) });
            var boxEntity = new SingleShapeEntity(box, new Vector3(0, 1, 1)) { State = { Name = "booox" } };

            boxEntity.InsertEntity(ring);

            SimulationEngine.GlobalInstancePort.Insert(boxEntity);

            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.3f)) { Material = new MaterialProperties("qq", 0.2f, 0.8f, 1.0f) }), new Vector3(0, 1, -0.5f)) { State = { Name = "booox2" } });
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.3f)) { Material = new MaterialProperties("qq", 0.2f, 0.8f, 1.0f) }), new Vector3(1, 1, 0.7f)) { State = { Name = "booox3" } });
        }

        void PopulateAckermanVehicleWithTail()
        {
            PopulateSimpleEnvironment();

            var box = new BoxShape(new BoxShapeProperties(10, new Pose(), new Vector3(1, 0.03f, 0.5f)) { Material = new MaterialProperties("ground", 0f, 0.5f, 0.5f) });
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(box, new Vector3(0, 0.02f, 2f)) { State = { Name = "booox" } });

            var vehicle = new AckermanVehicleExEntity("vehicle", new Vector3(0, 0.2f, 0), AckermanVehicles.Suspended4x4);

			var tail = new TailEntity.TailProperties
				{
					Origin = new Vector3(0, 0.1f, -0.20f),
					PayloadMass = 0.08f,
					PayloadRadius = 0.02f,
					TwistPower = 10000,
					ScanInterval = 0.025f,
					Segment1Length = 0.15f,
					Segment1Mass = 0.02f,
					Segment2Length = 0.2f,
					Segment2Mass = 0.02f,
					SegmentRadius = 0.01f,
					GroundRangefindersPositions = new[] { new Vector3(-0.06f, 0, 0.11f), new Vector3(0.06f, 0, 0.11f), new Vector3(0.06f, 0, -0.11f), new Vector3(-0.06f, 0, -0.11f) }
				}.Build("tail", vehicle);

			vehicle.InsertEntity(tail);

            SimulationEngine.GlobalInstancePort.Insert(vehicle);            
        }

		void PopulateEnvForGroundTail()
		{
            PopulateSimpleEnvironment();

            var vehicle = new AckermanVehicleExEntity("vehicle", new Vector3(0, 0.2f, 0), AckermanVehicles.Suspended4x4);

			var tail = new TailEntity.TailProperties
			{
				Origin = new Vector3(0, 0.1f, -0.20f),
				PayloadMass = 0.08f,
				PayloadRadius = 0.02f,
				TwistPower = 10000,
				ScanInterval = 0.025f,
				Segment1Length = 0.15f,
				Segment1Mass = 0.02f,
				Segment2Length = 0.2f,
				Segment2Mass = 0.02f,
				SegmentRadius = 0.01f,
				GroundRangefindersPositions = new[] { new Vector3(-0.06f, 0, 0.11f), new Vector3(0.06f, 0, 0.11f), new Vector3(0.06f, 0, -0.11f), new Vector3(-0.06f, 0, -0.11f) }
			}.Build("tail", vehicle);

			vehicle.InsertEntity(tail);

			SimulationEngine.GlobalInstancePort.Insert(vehicle);
		}

        void PopulateAckermanVehicle()
        {
            PopulateSimpleEnvironment();

            var box = new BoxShape(new BoxShapeProperties(10, new Pose(), new Vector3(1, 0.03f, 0.5f)) { Material = new MaterialProperties("ground", 0f, 0.5f, 0.5f) });
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(box, new Vector3(0, 0.02f, 2f)) { State = { Name = "booox" } });

            //var sav = new AckermanVehicleEntity("testee", new Vector3(0, 0.2f, 0), AckermanVehicles.Simplistic);
            //SimulationEngine.GlobalInstancePort.Insert(sav);
            var sav = new AckermanVehicleExEntity("testee", new Vector3(0, 0.2f, 0), AckermanVehicles.Simplistic);
            SimulationEngine.GlobalInstancePort.Insert(sav);
        }

        void PopulateStabilizer()
        {
            PopulateSimpleEnvironment();

			var box = new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.5f)) { Material = new MaterialProperties("qq", 0.2f, 0.8f, 1.0f) });
			//var boxEntity = new SingleShapeEntity(box, new Vector3()) { State = { Name = "booox", Pose = {Orientation = Quaternion.FromAxisAngle(1, 0, 0, (float)Math.PI / 4)}, Flags = EntitySimulationModifiers.IgnoreGravity} };
			var boxEntity = new SingleShapeEntity(box, new Vector3(0, 1, 0)) { State = { Name = "booox", Flags = EntitySimulationModifiers.IgnoreGravity } };

            var tail = new TailEntity.TailProperties
                    {
                        Origin = new Vector3(0, 0, -0.27f),
                        PayloadMass = 0.04f,
                        PayloadRadius = 0.02f,
                        TwistPower = 100,
						ScanInterval = 0.025f,
                        Segment1Length = 0.2f,
                        Segment1Mass = 0.02f,
                        Segment2Length = 0.2f,
                        Segment2Mass = 0.02f,
                        SegmentRadius = 0.01f,
						GroundRangefindersPositions = new[] { new Vector3(-0.06f, 0, 0.11f), new Vector3(0.06f, 0, 0.11f), new Vector3(0.06f, 0, -0.11f), new Vector3(-0.06f, 0, -0.11f) }
                    }.Build("tail", boxEntity);
            boxEntity.InsertEntity(tail);

			//var hangerEntity = new SingleShapeEntity(new SphereShape(new SphereShapeProperties(1, new Pose(), 0.1f)), new Vector3(0, 1, 0.5f))
			//	{
			//		State = {Name = "hanger", Flags = EntitySimulationModifiers.Kinematic}
			//	};

			//boxEntity.ParentJoint = new Joint
			//	{
			//		State = new JointProperties(
			//			new JointAngularProperties
			//				{
			//					//TwistMode = JointDOFMode.Free,
			//					Swing1Mode = JointDOFMode.Free,
			//					//Swing2Mode = JointDOFMode.Free
			//				},
			//			new EntityJointConnector(boxEntity, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0.1f, 0)) {EntityName = boxEntity.State.Name},
			//			new EntityJointConnector(hangerEntity, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, -0.5f, 0)) {EntityName = hangerEntity.State.Name}) 
			//			{Name = "Hanger joint"}
			//	};
			//hangerEntity.InsertEntity(boxEntity);
			
			//SimulationEngine.GlobalInstancePort.Insert(hangerEntity);
			SimulationEngine.GlobalInstancePort.Insert(boxEntity);
        }

        void GenerateTest()
        {
            var terrain = new TerrainEntity(@"terrain03.bmp", "terrain_tex.jpg", new MaterialProperties("ground", 0, 0.5f, 1.0f))
            {
                State = { Name = "Terrain", Assets = { Effect = "Terrain.fx" } },
            };
            SimulationEngine.GlobalInstancePort.Insert(terrain);

            var view = new CameraView { EyePosition = new Vector3(-12, 9, 6), LookAtPoint = new Vector3(0, 0, 6) };
            SimulationEngine.GlobalInstancePort.Update(view);

            var sky = new SkyDomeEntity("skydome.dds", "sky_diff.dds");
            SimulationEngine.GlobalInstancePort.Insert(sky);

            var sun = new LightSourceEntity
            {
                State = { Name = "Sun" },
                Type = LightSourceEntityType.Directional,
                Color = new Vector4(0.8f, 0.8f, 0.8f, 1),
                Direction = new Vector3(0.5f, -.75f, 0.5f)
            };
            SimulationEngine.GlobalInstancePort.Insert(sun);

            var sav = new AckermanVehicleExEntity("testee", new Vector3(), AckermanVehicles.Suspended4x4);
            SimulationEngine.GlobalInstancePort.Insert(sav);
        }

        void PopulateSimpleEnvironment()
        {
            var view = new CameraView { EyePosition = new Vector3(-1.65f, 1.63f, -0.29f), LookAtPoint = new Vector3(0, 0, 0) };
            SimulationEngine.GlobalInstancePort.Update(view);

            var sky = new SkyDomeEntity("skydome.dds", "sky_diff.dds");
            SimulationEngine.GlobalInstancePort.Insert(sky);

            var sun = new LightSourceEntity
            {
                Type = LightSourceEntityType.Directional,
                Color = new Vector4(0.8f, 0.8f, 0.8f, 1),
                Direction = new Vector3(0.5f, -.75f, 0.5f)
            };
            sun.State.Name = "Sun";
            SimulationEngine.GlobalInstancePort.Insert(sun);

            var ground = new HeightFieldEntity("Ground", "WoodFloor.dds", new MaterialProperties("ground", 0f, 0.5f, 0.5f));
            SimulationEngine.GlobalInstancePort.Insert(ground);
            //var ground = new TerrainEntity("terrain.bmp", "", new MaterialProperties("ground", 0f, 0.5f, 0.5f));
            //SimulationEngine.GlobalInstancePort.Insert(ground);
        }
    }
}
