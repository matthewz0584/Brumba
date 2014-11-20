using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            DC.Contract.Requires(zi.BetweenRL(0, (float)MaxRange));
            DC.Contract.Requires(i >= 0);
            DC.Contract.Requires(i < AngularRange / AngularResolution + 1);
            DC.Contract.Requires(OriginPose.Bearing.BetweenL(0, Constants.Pi2));
            DC.Contract.Requires(AngularResolution > 0);

			return OriginPose.Position + Vector2.Transform(new Vector2(zi, 0), Matrix.CreateRotationZ((float)(OriginPose.Bearing - AngularRange / 2 + i * AngularResolution)));
        }

        public RangefinderProperties Sparsify(int beams)
        {
            DC.Contract.Requires(beams <= AngularRange / AngularResolution + 1);
            DC.Contract.Ensures(DC.Contract.Result<RangefinderProperties>().AngularResolution >= DC.Contract.OldValue(AngularResolution));
            DC.Contract.Ensures(DC.Contract.Result<RangefinderProperties>().AngularResolution * beams >= AngularRange);

            return new RangefinderProperties
            {
                MaxRange = MaxRange,
                OriginPose = OriginPose,
                AngularRange = AngularRange,
                AngularResolution = AngularResolution * Math.Floor(AngularRange / AngularResolution / (beams - 1))
            };
        }

        public IEnumerable<Vector2> PreprocessMeasurements(IEnumerable<float> scan)
        {
            DC.Contract.Ensures(DC.Contract.Result<IEnumerable<Vector2>>() != null);

            var thi = this;
            return scan.Select((zi, i) => new {zi, i}).
                Where(p => !Precision.AlmostEqualWithAbsoluteError(p.zi, thi.MaxRange, p.zi - thi.MaxRange, 0.001)).
                Select(p => thi.BeamToVectorInRobotTransformation(p.zi, p.i));
        }
    }
}