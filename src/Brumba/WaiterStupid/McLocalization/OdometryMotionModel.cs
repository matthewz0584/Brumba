using System;
using System.Diagnostics.Contracts;
using Brumba.Utils;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class OdometryMotionModel : IPredictionModel<Vector3, Vector3>
    {
        readonly Vector2 _rotNoiseCoeffs;
        readonly Vector2 _transNoiseCoeffs;
        readonly Random _random;

        public OccupancyGrid Map { get; private set; }

        public OdometryMotionModel(OccupancyGrid map, Vector2 rotNoiseCoeffs, Vector2 transNoiseCoeffs)
        {
            Contract.Requires(map != null);

            Map = map;
            _rotNoiseCoeffs = rotNoiseCoeffs;
            _transNoiseCoeffs = transNoiseCoeffs;
            _random = new Random();
        }

        public Vector3 PredictParticleState(Vector3 particle, Vector3 control)
        {
			Contract.Ensures(!Map.Covers(Contract.Result<Vector3>().ExtractVector2()) || !Map[Contract.Result<Vector3>().ExtractVector2()]);
			Contract.Assume(!Map.Covers(particle.ExtractVector2()) || !Map[particle.ExtractVector2()]);

            var rotTransRot = OdometryToRotTransRotSequence(particle, control);

            var tries = 10;
            Vector3 prediction;
            do
            {
                prediction = particle + 
                    RotTransRotSequenceToOdometry(particle, rotTransRot + ComputeRotTransRotNoise(rotTransRot));
            }
            while (Map.Covers(prediction.ExtractVector2()) && Map[prediction.ExtractVector2()] && tries-- > 0);

	        return tries < 0 ? particle : prediction;
        }

        public static Vector3 OdometryToRotTransRotSequence(Vector3 particle, Vector3 control)
        {
			Contract.Ensures(Contract.Result<Vector3>().X > -Constants.Pi);
			Contract.Ensures(Contract.Result<Vector3>().X <= Constants.Pi);
			Contract.Ensures(Contract.Result<Vector3>().Z > -Constants.Pi);
			Contract.Ensures(Contract.Result<Vector3>().Z <= Constants.Pi);

            var rot1Delta = (float)Math.Atan2(control.Y, control.X) - particle.Z;
            var transDelta = new Vector2(control.X, control.Y).Length();
            var rot2Delta = control.Z - rot1Delta; //(particle.Z + control.Z) - (particle.Z + rot1Delta)            

			return new Vector3(rot1Delta.ToMinAbsValueAngle(), transDelta, rot2Delta.ToMinAbsValueAngle());
        }

        public static Vector3 RotTransRotSequenceToOdometry(Vector3 particle, Vector3 rotTransRot)
        {
			Contract.Ensures(Contract.Result<Vector3>().Z > -Constants.Pi);
			Contract.Ensures(Contract.Result<Vector3>().Z <= Constants.Pi);

            return new Vector3(rotTransRot.Y * (float)Math.Cos(particle.Z + rotTransRot.X),
                               rotTransRot.Y * (float)Math.Sin(particle.Z + rotTransRot.X),
                               (rotTransRot.X + rotTransRot.Z).ToMinAbsValueAngle());
        }

        public Vector3 ComputeRotTransRotNoise(Vector3 rotTransRot)
        {
            return new Vector3(
                (float)Normal.Sample(_random, 0,
                        Vector2.Dot(_rotNoiseCoeffs, new Vector2(Math.Abs(rotTransRot.X), rotTransRot.Y))),
                (float)Normal.Sample(_random, 0,
                        Vector2.Dot(_transNoiseCoeffs, new Vector2(rotTransRot.Y, Math.Abs(rotTransRot.X) + Math.Abs(rotTransRot.Z)))),
                (float)Normal.Sample(_random, 0,
                        Vector2.Dot(_rotNoiseCoeffs, new Vector2(Math.Abs(rotTransRot.Z), rotTransRot.Y))));
        }
    }
}