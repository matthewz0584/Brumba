using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework.Graphics;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using xVector4 = Microsoft.Xna.Framework.Vector4;

namespace Brumba.Simulation.SimulatedTail
{
    public class InfraredRfEntity : SingleShapeEntity
    {
        float _elapsedSinceLastScan;
        float _appTime;
        List<RaycastImpactPoint> _lastScanResult;
                
        VisualEntityMesh _impactMesh;
        CachedEffect _impactPointEffect;
        CachedEffectParameter _ipEffectTimeAttenuationHandle;

        [DataMember]
        public float MaximumRange { get; set; }

        [DataMember]
        public float Samples { get; set; }

        [DataMember]
        public float DispersionConeAngle { get; set; }

        [DataMember]
        public float ScanInterval { get; set; }

        public InfraredRfEntity()
            : base(new BoxShape(new BoxShapeProperties(0.005f, new Pose(), new Vector3(0.02f, 0.01f, 0.005f)) { DiffuseColor = new Vector4(0, 0, 0, 0)}), new Vector3())
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
            BoxShape.State.Name = name + " shape";
            State.Pose = initialPose;
        }

        public float Distance
        {
            get { return _lastScanResult.ToList().Select(ip => ip.Position.W).Concat(new[] { MaximumRange }).Min(); }
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

                CreateAndInsertPhysicsEntity(physicsEngine);

                InitImpactPointRendering(device);
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
            _lastScanResult.AddRange(ScanInPlane(horizontalPlane * Quaternion.CreateFromAxisAngle(new xVector3(0, 0, 1), (float)Math.PI / 2f)));
        }

        public override void Render(RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
            if (Flags.HasFlag(VisualEntityProperties.DisableRendering))
                return;

            base.Render(renderMode, transforms, currentCamera);

            RenderImpactPoints(renderMode, transforms);
        }

        void InitImpactPointRendering(GraphicsDevice device)
        {
            var hf = new HeightFieldShapeProperties("height field", 2, 0.01f, 2, 0.01f, 0, 0, 1, 1);
            hf.HeightSamples = Enumerable.Range(0, hf.RowCount * hf.ColumnCount).Select(_ => new HeightFieldSample()).ToArray();

            var mesh = SimulationEngine.ResourceCache.CreateMesh(device, hf);
            mesh.Textures[0] = SimulationEngine.ResourceCache.CreateTextureFromFile(device, "particle.bmp");
            _impactMesh = mesh;

            // we have a custom effect, with an additional global parameter. Get handle to it here
            _impactPointEffect = SimulationEngine.ResourceCache.CreateEffectFromFile(device, "LaserRangeFinder.fx");
            if (_impactPointEffect != null)
                _ipEffectTimeAttenuationHandle = _impactPointEffect.GetParameter("timeAttenuation");
        }

        void RenderImpactPoints(RenderMode renderMode, MatrixTransforms transforms)
        {
            var bs = Device.BlendState;
            var rs = Device.RasterizerState;
            var ds = Device.DepthStencilState;
            var oldEffect = Effect;
            
            Effect = _impactPointEffect;
            _ipEffectTimeAttenuationHandle.SetValue(new xVector4(100*(float) Math.Cos(_appTime*(1.0f/ScanInterval)), 0, 0, 1));

            foreach (var iPosition in _lastScanResult.Select(ip => new xVector3(ip.Position.X, ip.Position.Y, ip.Position.Z)))
            {
                var ipDir = xVector3.Normalize(iPosition - Position);
                var ipMeshNorm = new xVector3(0, 1, 0);
                transforms.World = Matrix.CreateFromAxisAngle(xVector3.Cross(ipMeshNorm, ipDir),
                                                              (float) Math.Acos(xVector3.Dot(ipDir, ipMeshNorm)));
                transforms.World.Translation = iPosition - 0.01f*ipDir;
                Render(renderMode, transforms, _impactMesh);
            }
            
            Effect = oldEffect;
            Device.BlendState = bs;
            Device.RasterizerState = rs;
            Device.DepthStencilState = ds;
        }

        List<RaycastImpactPoint> ScanInPlane(Quaternion planeOrientation)
        {
            var raycastProperties = new RaycastProperties
                {
                    StartAngle = -DispersionConeAngle / 2.0f,
                    EndAngle = DispersionConeAngle / 2.0f,
                    AngleIncrement = DispersionConeAngle / (Samples - 1),
                    Range = MaximumRange,
                    OriginPose = new Pose(TypeConversion.FromXNA(Position), TypeConversion.FromXNA(planeOrientation))
                };

            RaycastResult scanResult;
            PhysicsEngine.Raycast2D(raycastProperties).Test(out scanResult);
            return scanResult == null ? new List<RaycastImpactPoint>() : scanResult.ImpactPoints;
        }
    }
}