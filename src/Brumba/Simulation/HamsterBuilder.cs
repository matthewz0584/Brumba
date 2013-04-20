using System;
using System.Collections.Generic;
using Brumba.Simulation.SimulatedAckermanVehicle;
using Brumba.Simulation.SimulatedInfraredRfRing;
using Brumba.Simulation.SimulatedTurret;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;

namespace Brumba.Simulation
{
    public class HamsterBuilder
    {
        public HamsterBuilder()
        {
            AckermanVehicleProps = AckermanVehicles.Simplistic;
            InfraredRfRingProps = new InfraredRfRingProperties
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
            TurretProps = new TurretProperties
                {
                    BaseHeight = 0.03f,
                    BaseMass = 0.1f,
                    SegmentRadius = 0.015f,
                    TwistPower = 1000
                };
            CameraProps = new CameraProperties
                {
                    ViewSizeLength = 320,
                    ViewSizeHeight = 240,
                    ViewAngle = (float) Math.PI/4
                };
        }

        public AckermanVehicleProperties AckermanVehicleProps { set; get; }
        public InfraredRfRingProperties InfraredRfRingProps { set; get; }
        public TurretProperties TurretProps { set; get; }
        public CameraProperties CameraProps { set; get; }

        public AckermanVehicleExEntity Build()
        {
            var sav = new AckermanVehicleExEntity("Hamster", new Vector3(0, 0.2f, 0), AckermanVehicleProps);

            var ring = new InfraredRfRingEntity("Hamster Rfring", new Pose(new Vector3(0, 0.08f, 0)), InfraredRfRingProps);
            sav.InsertEntity(ring);

            var turret = new TurretEntity("Hamster Turret",
                            new Pose(new Vector3(0, AckermanVehicles.Simplistic.Clearance + AckermanVehicles.Simplistic.ChassisPartsProperties[0].Dimensions.Y + TurretProps.SegmentRadius, 0)),
                            TurretProps);
            sav.InsertEntity(turret);

            var camera = new CameraEntity(CameraProps.ViewSizeLength, CameraProps.ViewSizeHeight, CameraProps.ViewAngle, CameraEntity.CameraModelType.AttachedChild)
                {
                    State =
                        {
                            Name = "Hamster Camera",
                            Pose = new Pose(new Vector3(0, TurretProps.BaseHeight, TurretProps.SegmentRadius),
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

    [DataContract]
    public class CameraProperties
    {
        [DataMember]
        public int ViewSizeLength { get; set; }
        [DataMember]
        public int ViewSizeHeight { get; set; }
        [DataMember]
        public float ViewAngle { get; set; }
    }
}