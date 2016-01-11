using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using xMatrix = Microsoft.Xna.Framework.Matrix;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using xQuaternion = Microsoft.Xna.Framework.Quaternion;
using Vector2 = Microsoft.Robotics.PhysicalModel.Vector2;

namespace Brumba.Simulation.SimulatedInfraredRfRing
{
    public class InfraredRfHelper
    {
        readonly InfraredRfProperties _props;
        readonly xMatrix _localTransform;

        public InfraredRfHelper(Vector2 positionPolar, InfraredRfProperties props)
        {
            _props = props;
            _localTransform = PolarTransform(positionPolar);
            LastScanResult = new List<RaycastImpactPoint>();
        }

        public void Update(xMatrix world, PhysicsEngine physicsEngine)
        {
            LastScanResult = new List<RaycastImpactPoint>();
            // cast rays on a horizontal plane and again on a vertical plane
            // in order to make cast direction aligned with radial direction rotate it
            var horizontalPlane = xQuaternion.CreateFromRotationMatrix(xMatrix.CreateFromAxisAngle(new xVector3(0, 1, 0), (float)Math.PI / 2f) * GlobalTransfrom(world));
            LastScanResult.AddRange(ScanInPlane(horizontalPlane, GlobalTransfrom(world).Translation, physicsEngine));
            // cast rays on a vertical plane
            LastScanResult.AddRange(ScanInPlane(
                horizontalPlane * xQuaternion.CreateFromAxisAngle(new xVector3(0, 0, 1), (float) Math.PI/2f),
                GlobalTransfrom(world).Translation, physicsEngine));
        }

        public List<RaycastImpactPoint> LastScanResult { get; private set; }

        public float GetDistance()
        {
            return LastScanResult.ToList().Select(ip => ip.Position.W).Concat(new[] { _props.MaximumRange }).Min();
        }

        public xMatrix GlobalTransfrom(xMatrix world)
        {
            return _localTransform * world;
        }

        List<RaycastImpactPoint> ScanInPlane(xQuaternion planeOrientation, xVector3 origin, PhysicsEngine physicsEngine)
        {
            var raycastProperties = new RaycastProperties
                {
                    StartAngle = -_props.DispersionConeAngle / 2.0f,
                    EndAngle = _props.DispersionConeAngle / 2.0f,
                    AngleIncrement = _props.DispersionConeAngle / (_props.Samples - 1),
                    Range = _props.MaximumRange,
                    OriginPose = new Pose(TypeConversion.FromXNA(origin), TypeConversion.FromXNA(planeOrientation))
                };

            RaycastResult scanResult;
            physicsEngine.Raycast2D(raycastProperties).Test(out scanResult);
            return scanResult == null ? new List<RaycastImpactPoint>() : scanResult.ImpactPoints;
        }

        xMatrix PolarTransform(Vector2 polarCoord)
        {
            var rfRotation = xMatrix.CreateRotationY(polarCoord.X - MathHelper.PiOver2);
            var rfPosV = xVector3.Transform(new xVector3(polarCoord.Y, 0, 0), rfRotation);
            return rfRotation * xMatrix.CreateTranslation(rfPosV);
        }
    }
}