using System.ComponentModel;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using DC = System.Diagnostics.Contracts;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Xna.Framework;

namespace Brumba.McLrfLocalizer
{
    [DataContract]
    public struct RangefinderProperties
    {
        [DataMember]
        public double AngularResolution { get; set; }
        [DataMember]
        public double AngularRange { get; set; }
        [DataMember]
        public double MaxRange { get; set; }
        [DataMember]
		[Description("Position and bearing of rangefinder in robot's coordinate system. Bearing is direction of middle beam.")]
        public Pose OriginPose { get; set; }

        public Vector2 BeamToVectorInRobotTransformation(float zi, int i)
        {
            DC.Contract.Requires(zi >= 0);
            DC.Contract.Requires(zi <= MaxRange);
            DC.Contract.Requires(i >= 0);
            DC.Contract.Requires(i < AngularRange / AngularResolution + 1);
            DC.Contract.Requires(OriginPose.Bearing.Between(0, Constants.Pi2));
			DC.Contract.Requires(OriginPose.Position.Length() >= 0);
            DC.Contract.Requires(AngularResolution > 0);

			return OriginPose.Position + Vector2.Transform(new Vector2(zi, 0), Matrix.CreateRotationZ((float)(OriginPose.Bearing - AngularRange / 2 + i * AngularResolution)));
        }
    }
}