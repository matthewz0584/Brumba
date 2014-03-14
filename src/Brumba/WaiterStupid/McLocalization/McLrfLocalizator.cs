using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using Brumba.Utils;
using Brumba.WaiterStupid.McLocalization;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using Microsoft.Xna.Framework;

namespace Brumba.WaiterStupid.Tests
{
    public class McLrfLocalizator
    {
	    readonly ParticleFilter<Vector3, IEnumerable<float>, Vector3> _particleFilter;
	    readonly Random _random;

	    public McLrfLocalizator(OccupancyGrid map, RangefinderProperties rangefinderProperties, int particlesNumber)
	    {
			Contract.Requires(map != null);
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
		public ReadOnlyCollection<Vector3> Particles { get { return new ReadOnlyCollection<Vector3>(_particleFilter.Particles.ToList()); }}

	    public void InitPoseUnknown()
	    {
			Contract.Ensures(_particleFilter.Particles != null);
			Contract.Ensures(_particleFilter.Particles.Any());
			Contract.Ensures(_particleFilter.Particles.Count() <= ParticlesNumber);
			Contract.Ensures(Contract.ForAll(_particleFilter.Particles, p => Map.Covers(p.ExtractVector2()) && !Map[p.ExtractVector2()]));
			Contract.Ensures(Contract.ForAll(_particleFilter.Particles, p => p.Z > 0 && p.Z < MathHelper2.TwoPi));

		    var particlesInDimension = (int)Math.Pow(ParticlesNumber, 1d / 3d);
			_particleFilter.Init(Enumerable.
									Range(0, particlesInDimension * particlesInDimension).
									Select(i => new Vector3((float)ContinuousUniform.Sample(_random, 0, Map.SizeInMeters.X),
											  				(float)ContinuousUniform.Sample(_random, 0, Map.SizeInMeters.Y),
															(float)ContinuousUniform.Sample(_random, 0, 2 * Math.PI))).
									Where(p => !Map[p.ExtractVector2()]));
        }

		public void Update(Vector3 odometry, IEnumerable<float> scan)
		{
			Contract.Requires(scan != null);
			Contract.Requires(scan.Any());

			_particleFilter.Update(odometry, scan);
		}

        public Vector3 CalculatePoseExpectation()
        {
			return StatisticsForVectorSamples(_particleFilter.Particles, statistics => statistics.Mean);
		}

        public Vector3 CalculatePoseStdDev()
        {
			return StatisticsForVectorSamples(_particleFilter.Particles, statistics => statistics.StandardDeviation);
        }

		static Vector3 StatisticsForVectorSamples(IEnumerable<Vector3> vecs, Func<DescriptiveStatistics, double> func)
		{
			return new Vector3(
				(float)func(new DescriptiveStatistics(vecs.Select(vec => (double)vec.X))),
				(float)func(new DescriptiveStatistics(vecs.Select(vec => (double)vec.Y))),
				(float)func(new DescriptiveStatistics(vecs.Select(vec => (double)MathHelper2.ToPositiveAngle(vec.Z)))));
		}
    }
}