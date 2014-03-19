using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Brumba.Utils;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.McLocalization
{
    public class McLrfLocalizator
    {
        public const double THETA_BIN_SIZE = Math.PI / 18;

	    readonly ParticleFilter<Vector3, IEnumerable<float>, Vector3> _particleFilter;
        readonly Random _random;

        public McLrfLocalizator(OccupancyGrid map, RangefinderProperties rangefinderProperties, int particlesNumber)
	    {
			Contract.Requires(map != null);
			Contract.Requires(particlesNumber > 1);
			Contract.Ensures(Map == map);
			Contract.Ensures(_random != null);
			Contract.Ensures(_particleFilter != null);

		    Map = map;
		    ParticlesNumber = particlesNumber;
			_random = new Random();
		    _particleFilter = new ParticleFilter<Vector3, IEnumerable<float>, Vector3>(
			    new ResamplingWheel(),
			    new OdometryMotionModel(map, new Vector2(0.1f, 0.1f), new Vector2(0.1f, 0.1f)),
			    new LikelihoodFieldMeasurementModel(map, rangefinderProperties, 0.1f, 0.8f, 0.2f));
	    }

		public OccupancyGrid Map { get; private set; }
	    public int ParticlesNumber { get; private set; }
        public IEnumerable<Vector3> Particles { get { return _particleFilter.Particles; } }

        public void InitPoseUnknown()
	    {
			Contract.Ensures(Particles != null);
			Contract.Ensures(Particles.Any());
            Contract.Ensures(Particles.Count() == ParticlesNumber);
			Contract.Ensures(Contract.ForAll(Particles, p => Map.Covers(p.ExtractVector2()) && !Map[p.ExtractVector2()]));
            Contract.Ensures(Contract.ForAll(Particles, p => p.Z.Between(0, MathHelper.TwoPi)));

			_particleFilter.Init(Enumerable.Range(0, int.MaxValue).
									Select(i => new Vector3((float)ContinuousUniform.Sample(_random, 0, Map.SizeInMeters.X),
											  				(float)ContinuousUniform.Sample(_random, 0, Map.SizeInMeters.Y),
															((float)ContinuousUniform.Sample(_random, 0, 2 * MathHelper.Pi - float.Epsilon)))).
									Where(p => !Map[p.ExtractVector2()]).
                                    Take(ParticlesNumber));
        }

        public void InitPose(Vector3 poseMean, Vector3 poseStdDev)
        {
            Contract.Requires(Map.Covers(poseMean.ExtractVector2()));
            Contract.Requires(!Map[poseMean.ExtractVector2()]);
            Contract.Requires(poseStdDev.GreaterOrEqual(new Vector3()));
            Contract.Ensures(Contract.ForAll(Particles, p => Map.Covers(p.ExtractVector2()) && !Map[p.ExtractVector2()]));
            Contract.Ensures(Contract.ForAll(Particles, p => p.Z.Between(0, MathHelper.TwoPi)));

            _particleFilter.Init(Enumerable.Range(0, int.MaxValue).
                                    Select(i => new Vector3((float)Normal.Sample(_random, poseMean.X, poseStdDev.X),
                                                            (float)Normal.Sample(_random, poseMean.Y, poseStdDev.Y),
                                                            ((float)Normal.Sample(_random, poseMean.Z, poseStdDev.Z)).ToPositiveAngle())).
                                    Where(p => Map.Covers(p.ExtractVector2()) && !Map[p.ExtractVector2()]).
                                    Take(ParticlesNumber));
        }

		public void Update(Vector3 odometry, IEnumerable<float> scan)
		{
			Contract.Requires(scan != null);
			Contract.Requires(scan.Any());
            Contract.Requires(Particles != null);
            Contract.Requires(Particles.Any());
            Contract.Ensures(Particles.Count() == ParticlesNumber);
            Contract.Ensures(Contract.ForAll(Particles, p => !Map.Covers(p.ExtractVector2()) || (Map.Covers(p.ExtractVector2()) && !Map[p.ExtractVector2()])));
            Contract.Ensures(Contract.ForAll(Particles, p => p.Z.Between(0, MathHelper.TwoPi)));

			_particleFilter.Update(odometry, scan);
		}

        public Vector3 CalculatePoseMean()
        {
            Contract.Requires(Particles != null);
            Contract.Requires(Particles.Any());
            Contract.Ensures(Contract.Result<Vector3>().Z.Between(0, MathHelper.TwoPi));

            return new Vector3(
                MomentFor(Particles.Select(v => v.X), statistics => statistics.Mean),
                MomentFor(Particles.Select(v => v.Y), statistics => statistics.Mean),
                MathHelper2.AngleMean(Particles.Select(v => v.Z)));
		}

        public Vector3 CalculatePoseStdDev()
        {
            Contract.Requires(Particles != null);
            Contract.Requires(Particles.Any());
            Contract.Ensures(Contract.Result<Vector3>().GreaterOrEqual(new Vector3()));

			return new Vector3(
                MomentFor(Particles.Select(v => v.X), statistics => statistics.StandardDeviation),
                MomentFor(Particles.Select(v => v.Y), statistics => statistics.StandardDeviation),
                MomentFor(Particles.Select(v => v.Z), statistics => statistics.StandardDeviation));
        }

        public IEnumerable<Vector3> GetPoseCandidates()
        {
            Contract.Requires(Particles != null);
            Contract.Requires(Particles.Any());
            Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<Vector3>>(),
                p => Map.Covers(p.ExtractVector2()) && !Map[p.ExtractVector2()] && p.Z.Between(0, MathHelper.TwoPi)));

            var h = new PoseHistogram(Map, THETA_BIN_SIZE);
            h.Build(Particles);
            return h.Bins.Where(pb => pb.Samples.Any()).OrderByDescending(pb => pb.Samples.Count()).Select(pb => pb.PoseMean());
        }

		static float MomentFor(IEnumerable<float> floats, Func<DescriptiveStatistics, double> func)
		{
            Contract.Requires(floats != null);
            Contract.Requires(func != null);

		    return (float) func(new DescriptiveStatistics(floats.Select(f => (double) f)));
		}
    }
}