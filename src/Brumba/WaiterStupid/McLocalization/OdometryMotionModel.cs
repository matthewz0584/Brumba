using System;
using System.Diagnostics.Contracts;
using Brumba.Utils;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class OdometryMotionModel : IPredictionModel<Pose, Pose>
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

        public Pose PredictParticleState(Pose particle, Pose control)
        {
			Contract.Ensures(!Map.Covers(Contract.Result<Pose>().Position) || !Map[Contract.Result<Pose>().Position]);
            Contract.Ensures(Contract.Result<Pose>().Bearing.Between(0, Constants.Pi2));
			Contract.Assume(!Map.Covers(particle.Position) || !Map[particle.Position]);

            var rotTransRot = OdometryToRotTransRotSequence(particle, control);

            var tries = 10;
            Pose prediction;
            do
            {
                var noisyDelta = RotTransRotSequenceToOdometry(particle, rotTransRot + ComputeRotTransRotNoise(rotTransRot));
                prediction = new Pose(particle.Position + noisyDelta.Position, (particle.Bearing + noisyDelta.Bearing).ToPositiveAngle());
            } while (Map.Covers(prediction.Position) && Map[prediction.Position] && tries-- > 0);

	        return tries < 0 ? particle : prediction;
        }

        public static Vector3 OdometryToRotTransRotSequence(Pose particle, Pose control)
        {
			Contract.Ensures(Contract.Result<Vector3>().X.Between(-MathHelper.Pi, MathHelper.Pi));
            Contract.Ensures(Contract.Result<Vector3>().Z.Between(-MathHelper.Pi, MathHelper.Pi));

            var rot1Delta = Math.Atan2(control.Position.Y, control.Position.X) - particle.Bearing;
            var transDelta = control.Position.Length();
            var rot2Delta = control.Bearing - rot1Delta; //(particle.Z + control.Z) - (particle.Z + rot1Delta)            

            return new Vector3((float)rot1Delta.ToMinAbsValueAngle(), transDelta, (float)rot2Delta.ToMinAbsValueAngle());
        }

        public static Pose RotTransRotSequenceToOdometry(Pose particle, Vector3 rotTransRot)
        {
            Contract.Ensures(Contract.Result<Pose>().Bearing.Between(0, Constants.Pi2));

            return new Pose(new Vector2(rotTransRot.Y * (float)Math.Cos(particle.Bearing + rotTransRot.X),
                                        rotTransRot.Y * (float)Math.Sin(particle.Bearing + rotTransRot.X)),
                               (rotTransRot.X + rotTransRot.Z).ToPositiveAngle());
        }

        public Vector3 ComputeRotTransRotNoise(Vector3 rotTransRot)
        {
            Contract.Requires(rotTransRot.X.Between(-MathHelper.Pi, MathHelper.Pi));
            Contract.Requires(rotTransRot.Z.Between(-MathHelper.Pi, MathHelper.Pi));

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