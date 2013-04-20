using System;
using Brumba.Simulation.SimulatedAckermanVehicle;
using Brumba.Simulation.SimulatedInfraredRfRing;
using Brumba.Simulation.SimulatedTurret;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation
{
    public class HamsterBuilder
    {
        public static AckermanVehicleExEntity BuildHamster()
        {
            var sav = new AckermanVehicleExEntity("hamster", new Vector3(0, 0.2f, 0), AckermanVehicles.Simplistic);

            var ring = new InfraredRfRingEntity("rfring", new Pose(new Vector3(0, 0.08f, 0)),
                                                new InfraredRfProperties
                                                    {
                                                        DispersionConeAngle = 4f,
                                                        Samples = 3f,
                                                        MaximumRange = 1,
                                                        ScanInterval = 0.1f
                                                    })
                {
                    RfPositionsPolar =
                        {
                            new Vector2(0, 0.08f),
                            new Vector2((float) Math.PI*1/6, 0.085f),
                            new Vector2((float) Math.PI*2/6, 0.07f),
                            new Vector2((float) Math.PI*3/6, 0.06f),
                            new Vector2((float) Math.PI, 0.1f),
                            new Vector2((float) Math.PI*9/6, 0.06f),
                            new Vector2((float) Math.PI*10/6, 0.07f),
                            new Vector2((float) Math.PI*11/6, 0.085f),
                        }
                };
            sav.InsertEntity(ring);

            var turretProps = new TurretEntity.Properties
                {
                    BaseHeight = 0.03f,
                    BaseMass = 0.1f,
                    SegmentRadius = 0.015f,
                    TwistPower = 1000
                };

            var turret = new TurretEntity("turret",
                                          new Pose(new Vector3(0,
                                                               AckermanVehicles.Simplistic.Clearance +
                                                               AckermanVehicles.Simplistic.ChassisPartsProperties[0].Dimensions.Y + turretProps.SegmentRadius, 0)),
                                          turretProps);
            TurretEntity.Builder.Build(turret, sav);
            sav.InsertEntity(turret);

            var camera = new CameraEntity(320, 240, (float)Math.PI / 4, CameraEntity.CameraModelType.AttachedChild)
                {
                    State =
                        {
                            Name = "camera",
                            Pose = new Pose(new Vector3(0, turretProps.BaseHeight, turretProps.SegmentRadius),
                                         Quaternion.FromAxisAngle(0, 1, 0, (float) Math.PI)),
                            Assets = {Mesh = "WebCam.obj"}
                        },
                    IsPhysicsVisible = true,
                    IsRealTimeCamera = true,
                    MeshScale = new Vector3(0.75f, 0.75f, 0.75f),
                };
            turret.InsertEntity(camera);
            return sav;
        }
    }
}