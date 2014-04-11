using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using Brumba.MapProvider;
using Brumba.Simulation.SimulatedAckermanVehicle;
using Brumba.Simulation.SimulatedInfraredRfRing;
using Brumba.Simulation.SimulatedLrf;
using Brumba.Simulation.SimulatedReferencePlatform2011;
using Brumba.Simulation.SimulatedTail;
using Brumba.Simulation.SimulatedTimer;
using Brumba.Simulation.SimulatedTurret;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Xna.Framework;
using W3C.Soap;
using Color = Microsoft.Xna.Framework.Color;
using Quaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using Vector2 = Microsoft.Robotics.PhysicalModel.Vector2;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;
using Vector4 = Microsoft.Robotics.PhysicalModel.Vector4;

namespace Brumba.Simulation.EnvironmentBuilder
{
    [Contract(Contract.Identifier)]
    [DisplayName("EnvironmentBuilder")]
    [Description("EnvironmentBuilder service (no description provided)")]
    class EnvironmentBuilderService : DsspServiceBase
	{
#pragma warning disable 0649
		[ServiceState]
		[InitialStatePartner(Optional = true)]
        EnvironmentBuilderState _state;
#pragma warning restore 0649

		[ServicePort("/EnvironmentBuilder", AllowMultipleInstances = true)]
        EnvironmentBuilderOperations _mainPort = new EnvironmentBuilderOperations();

        [Partner("Engine", Contract = Microsoft.Robotics.Simulation.Engine.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
		SimulationEnginePort _engineStub = new SimulationEnginePort();//only for auto engine creation

        public EnvironmentBuilderService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        //TurretEntity _turret;
        //[Partner("Turret", Contract = SimulatedTurret.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
        //SimulatedTurret.Proxy.SimulatedTurretOperations _tPort = new SimulatedTurret.Proxy.SimulatedTurretOperations();

		//[Partner("Waiter", Contract = Microsoft.Robotics.Services.Drive.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		//Microsoft.Robotics.Services.Drive.Proxy.DriveOperations _waiter = new Microsoft.Robotics.Services.Drive.Proxy.DriveOperations();

		//[Partner("qq", Contract = SimulatedReferencePlatform2011.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        //SimulatedReferencePlatform2011.Proxy.ReferencePlatform2011Operations _dummy = new SimulatedReferencePlatform2011.Proxy.ReferencePlatform2011Operations();

		//[Partner("qq", Contract = SimulatedLrf.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
		//Brumba.Simulation.SimulatedLrf.Operations _dummy = new Brumba.Simulation.SimulatedLrf.Operations();

        //[Partner("qq", Contract = Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        //Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.SickLRFOperations _dummy = new Microsoft.Robotics.Services.Sensors.SickLRF.Proxy.SickLRFOperations();

        protected override void Start()
        {
			//CrossCountryGenerator.Generate(257, 0.1f).Save("terrain00.bmp");
			//PopulateStabilizer();
			//PopulateAckermanVehicle();
			//GenerateTest();
			//PopulateAckermanVehicleWithTail();
	        //PopulateEnvForGroundTail();
            //PopulatePuckRobot();
            //PopulateInfraredRfRing();
            //PopulateTurret();
            //PopulateHamster();
	        //PopulateRefPlatformSimpleTests();
			//PopulateMcLrfLocalizerTests();
	        
            base.Start();

            //Thread.Sleep(5000);
            //_tPort.SetBaseAngle((float) Math.PI/4);
            //_turret.BaseAngle = (float)Math.PI / 4;
        }

	    void PopulateMcLrfLocalizerTests()
	    {
			PopulateSimpleEnvironment();

			SimulationEngine.GlobalInstancePort.Insert(BuildWaiter1("stupid_waiter"));
		    _mainPort.P3.Post(new BuildBoxWorld {ResponsePort = new PortSet<DefaultSubmitResponseType, Fault>()});
	    }

	    private void PopulateRefPlatformSimpleTests()
	    {
			PopulateSimpleEnvironment();

	        SimulationEngine.GlobalInstancePort.Insert(BuildWaiter1("stupid_waiter"));
			SimulationEngine.GlobalInstancePort.Insert(new TimerEntity("timer"));
			SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1.0f, new Pose(), new Vector3(1, 1, 1))), new Vector3(8, 0.501f, 0)) { State = { Name = "golden_brick_out_of_range" } });
			SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1.0f, new Pose(), new Vector3(1, 1, 1))), new Vector3(-5f, 0.501f, 0)) { State = { Name = "golden_brick_in_range" } });
	    }

        private static ReferencePlatform2011Entity BuildWaiter1(string waiterName)
        {
            var refPlatform = new ReferencePlatform2011Entity {State = {Name = waiterName, Pose = new Pose(new Vector3(), Quaternion.FromAxisAngle(0, 1, 0, MathHelper.Pi))}};
            var lidar = new LaserRangeFinderExEntity
                {
                    State = {Name = waiterName + "_lidar"},
                    LaserBox = new BoxShape(new BoxShapeProperties(0.16f, new Pose(new Vector3(0, 0.2f, -0.2f)),
                                                                   new Vector3(0.04f, 0.07f, 0.04f))),
                    RaycastProperties = new RaycastProperties
                        {
                            StartAngle = -120,
                            EndAngle = +120,
                            AngleIncrement = 0.36f,
                            Range = 5.6f,
                            OriginPose = new Pose(),
							ScanInterval = 0.1f
                        }
                };

            refPlatform.InsertEntity(lidar);
            return refPlatform;
        }

        void PopulateHamster()
        {
            PopulateSimpleEnvironment();

            var hamster = new HamsterBuilder().Build();

            SimulationEngine.GlobalInstancePort.Insert(hamster);

            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.2f))), new Vector3(0, 0.21f, 1.5f)) { State = { Name = "wall1" } });
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.2f))), new Vector3(1.31f, 0.21f, 1f)) { State = { Name = "wall2" } });
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.2f))), new Vector3(0, 0.21f, 0.5f)) { State = { Name = "wall3" } });
        }

        void PopulateTurret()
        {
            PopulateSimpleEnvironment();

            var turretOwner = new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.2f))), new Vector3(0, 0.21f, 0)) { State = { Name = "turret owner" } };

            var turretProps = new TurretProperties
            {
                BaseHeight = 0.03f,
                BaseMass = 0.1f,
                SegmentRadius = 0.015f,
                TwistPower = 1000
            };
            var turret = new TurretEntity("turret",
                        new Pose(new Vector3(0, 0.1f + turretProps.BaseHeight / 2, 0)), turretProps);

            turret.InsertEntity(new SingleShapeEntity(
                    new BoxShape(new BoxShapeProperties(0.05f, new Pose(new Vector3(0, turretProps.BaseHeight/2, 0)),
                                                        new Vector3(0.01f, 0.01f, 0.1f))), new Vector3())
                    { State = {Name = "turret flag"} });

            turretOwner.InsertEntity(turret);

            SimulationEngine.GlobalInstancePort.Insert(turretOwner);
        }

        void PopulateInfraredRfRing()
        {
            PopulateSimpleEnvironment();

            var ring = new InfraredRfRingEntity("rfring", new Pose(new Vector3()),
                                                new InfraredRfRingProperties
                                                    {
                                                        InfraredRfProperties = new InfraredRfProperties
                                                            {
                                                                DispersionConeAngle = 4f,
                                                                Samples = 3f,
                                                                MaximumRange = 1,
                                                                ScanInterval = 0.1f
                                                            },
                                                        RfPositionsPolar = new List<Vector2>
                                                            {
                                                                new Vector2(0, 0.1f),
                                                                new Vector2((float) Math.PI/2, 0.2f),
                                                                new Vector2((float) Math.PI, 0.3f),
                                                                new Vector2((float) Math.PI*3/2, 0.4f)
                                                            }
                                                    });

            var ringOwner = new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.2f))), new Vector3(0, 0.21f, 1)) { State = { Name = "ring owner" } };

            ringOwner.InsertEntity(ring);

            SimulationEngine.GlobalInstancePort.Insert(ringOwner);
            
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.2f))), new Vector3(0, 0.21f, 1.5f)) { State = { Name = "wall1" } });
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.2f))), new Vector3(1.31f, 0.21f, 1f)) { State = { Name = "wall2" } });
            SimulationEngine.GlobalInstancePort.Insert(new SingleShapeEntity(new BoxShape(new BoxShapeProperties(1, new Pose(), new Vector3(0.2f, 0.2f, 0.2f))), new Vector3(0, 0.21f, 0.5f)) { State = { Name = "wall3" } });
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

	    [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
		public IEnumerator<ITask> OnBuildBoxWorld(BuildBoxWorld bbwRq)
	    {
			if (_state == null)
				bbwRq.ResponsePort.Post(new Fault {Reason = new [] {new ReasonText {Value = "There is no state to build world from."}}});

		    var bwb = new BoxWorldParser(_state.BoxWorldParserSettings, new PixelBlockGlue(), new PixelColorClassifier());

			var mapImageFile = Path.IsPathRooted(_state.MapImageFile)
                ? Path.Combine(LayoutPaths.RootDir, _state.MapImageFile.Trim(Path.DirectorySeparatorChar))
                : Path.Combine(LayoutPaths.RootDir, LayoutPaths.MediaDir, _state.MapImageFile);

		    var boxes = bwb.ParseBoxes((Bitmap) Image.FromFile(mapImageFile));
		    foreach (var box in boxes)
		    {
				var insRequest = new InsertSimulationEntity(box);
				SimulationEngine.GlobalInstancePort.Post(insRequest);
				yield return insRequest.ResponsePort.Receive((DefaultInsertResponseType success) => {});
		    }

			bbwRq.ResponsePort.Post(new DefaultSubmitResponseType());
	    }
    }
}
