using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework.Graphics;
using Xna = Microsoft.Xna.Framework;

namespace Brumba.Simulation.SimulatedStabilizer
{
    public class InfraredRfEntity : VisualEntity
    {
        float _elapsedSinceLastScan;
        float _appTime;
        List<RaycastImpactPoint> _lastScanResult;
        CachedEffectParameter _timeAttenuationHandle;

        [DataMember]
        public float MaximumRange { get; set; }

        [DataMember]
        public float Samples { get; set; }

        [DataMember]
        public float DispersionConeAngle { get; set; }

        [DataMember]
        public float ScanInterval { get; set; }

        public InfraredRfEntity()
        {
            _lastScanResult = new List<RaycastImpactPoint>();

            DispersionConeAngle = 4f;
            Samples = 3f;
            MaximumRange = 1;
            ScanInterval = 0.025f;
        }

        public InfraredRfEntity(string name, Pose initialPose)
            : this()
        {
            State.Name = name;
            State.Pose = initialPose;

            // used for rendering impact points
            State.Assets.Effect = "LaserRangeFinder.fx";
        }

        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            try
            {
                if (Parent == null)
                    throw new Exception("This entity must be a child of another entity.");

                // set flag so rendering engine renders us last
                Flags |= VisualEntityProperties.UsesAlphaBlending;

                base.Initialize(device, physicsEngine);

                // The mesh is used to render the ray impact points rather than the sensor geometry.
                Meshes.Add(CreateImpactPointMesh(device));

                // we have a custom effect, with an additional global parameter. Get handle to it here
                if (Effect != null)
                    _timeAttenuationHandle = Effect.GetParameter("timeAttenuation");
            }
            catch (Exception ex)
            {
                HasBeenInitialized = false;
                InitError = ex.ToString();
            }
        }

        public override void Update(FrameUpdate update)
        {
            base.Update(update);
            
            _appTime = (float)update.ApplicationTime;

            if ((_elapsedSinceLastScan += (float)update.ElapsedTime) < ScanInterval) return;

            _elapsedSinceLastScan = 0;

            _lastScanResult = new List<RaycastImpactPoint>();
            // cast rays on a horizontal plane and again on a vertical plane
            var horizontalPlane = TypeConversion.ToXNA(Parent.State.Pose.Orientation) * TypeConversion.ToXNA(State.Pose.Orientation);
            _lastScanResult.AddRange(ScanInPlane(horizontalPlane));
            _lastScanResult.AddRange(ScanInPlane(horizontalPlane * Xna.Quaternion.CreateFromAxisAngle(new Xna.Vector3(0, 0, 1), (float)Math.PI / 2f)));
        }

        public override void Render(RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
            if (Flags.HasFlag(VisualEntityProperties.DisableRendering))
                return;

            _timeAttenuationHandle.SetValue(new Xna.Vector4(100 * (float)Math.Cos(_appTime * (1.0f / ScanInterval)), 0, 0, 1));

            //sticker in the place of rf device - I could not render it here better and easy
            base.Render(renderMode, transforms, currentCamera);

            foreach (var iPosition in _lastScanResult.Select(ip => new Xna.Vector3(ip.Position.X, ip.Position.Y, ip.Position.Z)))
            {
                var ipDir = Xna.Vector3.Normalize(iPosition - RaysOrigin());
                var ipMeshNorm = new Xna.Vector3(0, 1, 0);
                transforms.World = Xna.Matrix.CreateFromAxisAngle(Xna.Vector3.Cross(ipMeshNorm, ipDir),
                                                          (float)Math.Acos(Xna.Vector3.Dot(ipDir, ipMeshNorm)));
                transforms.World.Translation = iPosition - 0.01f * ipDir;
                Render(renderMode, transforms, Meshes[0]);
            }
        }

        public float Distance
        {
            get { return _lastScanResult.Select(ip => ip.Position.W).Concat(new[] { MaximumRange }).Min(); }
        }

        VisualEntityMesh CreateImpactPointMesh(GraphicsDevice device)
        {
            var hf = new HeightFieldShapeProperties("height field", 2, 0.01f, 2, 0.01f, 0, 0, 1, 1);
            hf.HeightSamples = Enumerable.Range(0, hf.RowCount * hf.ColumnCount).Select(_ => new HeightFieldSample()).ToArray();

            var mesh = SimulationEngine.ResourceCache.CreateMesh(device, hf);
            mesh.Textures[0] = SimulationEngine.ResourceCache.CreateTextureFromFile(device, "particle.bmp");
            return mesh;
        }

        List<RaycastImpactPoint> ScanInPlane(Xna.Quaternion planeOrientation)
        {
            var raycastProperties = new RaycastProperties
                {
                    StartAngle = -DispersionConeAngle / 2.0f,
                    EndAngle = DispersionConeAngle / 2.0f,
                    AngleIncrement = DispersionConeAngle / (Samples - 1),
                    Range = MaximumRange,
                    OriginPose = new Pose(TypeConversion.FromXNA(RaysOrigin()), TypeConversion.FromXNA(planeOrientation))
                };

            RaycastResult scanResult;
            PhysicsEngine.Raycast2D(raycastProperties).Test(out scanResult);
            return scanResult == null ? new List<RaycastImpactPoint>() : scanResult.ImpactPoints;
        }

        Xna.Vector3 RaysOrigin()
        {
            return Xna.Vector3.Transform(TypeConversion.ToXNA(State.Pose.Position), Parent.World);
        }
    }
}