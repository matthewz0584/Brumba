﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Robotics.PhysicalModel.Vector2;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;
using Vector4 = Microsoft.Robotics.PhysicalModel.Vector4;
using xMatrix = Microsoft.Xna.Framework.Matrix;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using xVector4 = Microsoft.Xna.Framework.Vector4;

namespace Brumba.Simulation.SimulatedInfraredRfRing
{
    [DataContract]
    public class InfraredRfRingEntity : VisualEntity
    {
        float _elapsedSinceLastScan;
        float _appTime;

        VisualEntityMesh _rfBody;
        VisualEntityMesh _impactMesh;
        CachedEffect _impactPointEffect;
        CachedEffectParameter _ipEffectTimeAttenuationHandle;

        List<InfraredRfHelper> _rfHelpers;

        [DataMember]
        public InfraredRfRingProperties Props { get; set; }

        public InfraredRfRingEntity()
        {
        }

        public InfraredRfRingEntity(string name, Pose initialPose, InfraredRfRingProperties props)
            : this()
        {
            Props = props;
            State.Name = name;
            State.Pose = initialPose;
        }

        public float[] GetDistances()
        {
            return _rfHelpers.Select(rfh => rfh.GetDistance()).ToArray();
        }

        public override void Initialize(GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            try
            {
                if (Parent == null)
                    throw new Exception("This entity must be a child of another entity.");
                // set flag so rendering engine renders us last
                Flags |= VisualEntityProperties.UsesAlphaBlending;

                _rfHelpers = Props.RfPositionsPolar.Select(rfPos => new InfraredRfHelper(rfPos, Props.InfraredRfProperties)).ToList();

                base.Initialize(device, physicsEngine);

                InitBodyRendering(device);

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

            if ((_elapsedSinceLastScan += (float)update.ElapsedTime) < Props.InfraredRfProperties.ScanInterval)
                return;

            _elapsedSinceLastScan = 0;

            foreach (var rfHelper in _rfHelpers)
                rfHelper.Update(World, PhysicsEngine);
        }

        public override void Render(RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
            if (Flags.HasFlag(VisualEntityProperties.DisableRendering))
                return;

            RenderBodies(renderMode, transforms);

            RenderAllRfImpactPoints(renderMode, transforms);
        }

        void RenderBodies(RenderMode renderMode, MatrixTransforms transforms)
        {
            foreach (var rfHelper in _rfHelpers)
            {
                transforms.World = rfHelper.GlobalTransfrom(World);
                Render(renderMode, transforms, _rfBody);
            }
        }

        void RenderAllRfImpactPoints(RenderMode renderMode, MatrixTransforms transforms)
        {
            var bs = Device.BlendState;
            var rs = Device.RasterizerState;
            var ds = Device.DepthStencilState;
            var oldEffect = Effect;

            Effect = _impactPointEffect;
            _ipEffectTimeAttenuationHandle.SetValue(new xVector4(100 * (float)Math.Cos(_appTime * (1.0f / Props.InfraredRfProperties.ScanInterval)), 0, 0, 1));

            foreach (var rfHelper in _rfHelpers)
                RenderRfImpactPoints(renderMode, transforms, rfHelper);

            Effect = oldEffect;
            Device.BlendState = bs;
            Device.RasterizerState = rs;
            Device.DepthStencilState = ds;
        }

        void RenderRfImpactPoints(RenderMode renderMode, MatrixTransforms transforms, InfraredRfHelper rfHelper)
        {
            foreach (var iPosition in rfHelper.LastScanResult.Select(ip => new xVector3(ip.Position.X, ip.Position.Y, ip.Position.Z)))
            {
                var ipDir = xVector3.Normalize(iPosition - rfHelper.GlobalTransfrom(World).Translation);
                var ipMeshNorm = new xVector3(0, 1, 0);
                transforms.World = xMatrix.CreateFromAxisAngle(xVector3.Cross(ipMeshNorm, ipDir),
                                                              (float)Math.Acos(xVector3.Dot(ipDir, ipMeshNorm)));
                transforms.World.Translation = iPosition - 0.01f * ipDir;
                Render(renderMode, transforms, _impactMesh);
            }
        }

        void InitBodyRendering(GraphicsDevice device)
        {
            _rfBody = SimulationEngine.ResourceCache.CreateMesh(device,
                new BoxShapeProperties(0.005f, new Pose(), new Vector3(0.005f, 0.01f, 0.02f)) { DiffuseColor = new Vector4(0, 0, 0, 0) });
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