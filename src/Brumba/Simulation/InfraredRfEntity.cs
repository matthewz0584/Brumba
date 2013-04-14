using System;
using System.Linq;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework.Graphics;
using xMatrix = Microsoft.Xna.Framework.Matrix;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using xVector4 = Microsoft.Xna.Framework.Vector4;

namespace Brumba.Simulation.SimulatedTail
{
    public class InfraredRfEntity : VisualEntity
    {
        float _elapsedSinceLastScan;
        float _appTime;
                
        VisualEntityMesh _impactMesh;
        CachedEffect _impactPointEffect;
        CachedEffectParameter _ipEffectTimeAttenuationHandle;
        private InfraredRfHelper _rfHelper;

        [DataMember]
        public InfraredRfProperties Props { get; set; }

        public InfraredRfEntity()
        {
        }

        public InfraredRfEntity(string name, Pose initialPose, InfraredRfProperties props)
            : this()
        {
            Props = props;
            State.Name = name;
            State.Pose = initialPose;
        }

        public float Distance
        {
            get { return _rfHelper.GetDistance(); }
        }

        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            try
            {
                if (Parent == null)
                    throw new Exception("This entity must be a child of another entity.");
                // set flag so rendering engine renders us last
                Flags |= VisualEntityProperties.UsesAlphaBlending;

                _rfHelper = new InfraredRfHelper(new Vector2(0, 0.001f), Props);

                base.Initialize(device, physicsEngine);

                Meshes.Add(SimulationEngine.ResourceCache.CreateMesh(device, new BoxShapeProperties(0.005f, new Pose(), new Vector3(0.02f, 0.01f, 0.005f)) { DiffuseColor = new Vector4(0, 0, 0, 0) }));

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

            if ((_elapsedSinceLastScan += (float)update.ElapsedTime) < Props.ScanInterval)
                return;

            _elapsedSinceLastScan = 0;

            _rfHelper.Update(World, PhysicsEngine);
        }

        public override void Render(RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
            if (Flags.HasFlag(VisualEntityProperties.DisableRendering))
                return;

            base.Render(renderMode, transforms, currentCamera);

            RenderImpactPoints(renderMode, transforms);
        }

        void RenderImpactPoints(RenderMode renderMode, MatrixTransforms transforms)
        {
            var bs = Device.BlendState;
            var rs = Device.RasterizerState;
            var ds = Device.DepthStencilState;
            var oldEffect = Effect;

            Effect = _impactPointEffect;
            _ipEffectTimeAttenuationHandle.SetValue(new xVector4(100 * (float)Math.Cos(_appTime * (1.0f / Props.ScanInterval)), 0, 0, 1));

            foreach (var iPosition in _rfHelper.LastScanResult.Select(ip => new xVector3(ip.Position.X, ip.Position.Y, ip.Position.Z)))
            {
                var ipDir = xVector3.Normalize(iPosition - _rfHelper.GlobalTransfrom(World).Translation);
                var ipMeshNorm = new xVector3(0, 1, 0);
                transforms.World = xMatrix.CreateFromAxisAngle(xVector3.Cross(ipMeshNorm, ipDir),
                                                              (float)Math.Acos(xVector3.Dot(ipDir, ipMeshNorm)));
                transforms.World.Translation = iPosition - 0.01f * ipDir;
                Render(renderMode, transforms, _impactMesh);
            }

            Effect = oldEffect;
            Device.BlendState = bs;
            Device.RasterizerState = rs;
            Device.DepthStencilState = ds;
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
    }
}