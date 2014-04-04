using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.MapProvider;
using Brumba.Utils;
using Brumba.WaiterStupid;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using Microsoft.Xna.Framework;
using DC = System.Diagnostics.Contracts;

namespace Brumba.McLrfLocalizer
{
    public class McLrfLocalizer
    {
        public const double THETA_BIN_SIZE = Math.PI / 18;

        readonly ParticleFilter<Pose, IEnumerable<float>, Pose> _particleFilter;
        readonly Random _random;

        public McLrfLocalizer(OccupancyGrid map, RangefinderProperties rangefinderProperties, int particlesNumber)
	    {
			DC.Contract.Requires(map != null);
			DC.Contract.Requires(particlesNumber > 1);
			DC.Contract.Ensures(Map == map);
			DC.Contract.Ensures(_random != null);
			DC.Contract.Ensures(_particleFilter != null);

		    Map = map;
		    ParticlesNumber = particlesNumber;
			_random = new Random();
            _particleFilter = new ParticleFilter<Pose, IEnumerable<float>, Pose>(
			    new ResamplingWheel(),
			    new OdometryMotionModel(map, new Vector2(0.1f, 0.1f), new Vector2(0.1f, 0.1f)),
			    new LikelihoodFieldMeasurementModel(map, rangefinderProperties, 0.1f, 0.8f, 0.2f));
	    }

		public OccupancyGrid Map { get; private set; }
	    public int ParticlesNumber { get; private set; }
        public IEnumerable<Pose> Particles { get { return _particleFilter.Particles; } }

        public void InitPoseUnknown()
	    {
			DC.Contract.Ensures(Particles != null);
			DC.Contract.Ensures(Particles.Any());
            DC.Contract.Ensures(Particles.Count() == ParticlesNumber);
			DC.Contract.Ensures(DC.Contract.ForAll(Particles, p => Map.Covers(p.Position) && !Map[p.Position]));
            DC.Contract.Ensures(DC.Contract.ForAll(Particles, p => p.Bearing.Between(0, Constants.Pi2)));

			_particleFilter.Init(Enumerable.Range(0, int.MaxValue).
									Select(i => new Pose(new Vector2((float)ContinuousUniform.Sample(_random, 0, Map.SizeInMeters.X),
											  				         (float)ContinuousUniform.Sample(_random, 0, Map.SizeInMeters.Y)),
															((float)ContinuousUniform.Sample(_random, 0, Constants.Pi2 - double.Epsilon)))).
									Where(p => !Map[p.Position]).
                                    Take(ParticlesNumber));
        }

        public void InitPose(Pose poseMean, Pose poseStdDev)
        {
            DC.Contract.Requires(Map.Covers(poseMean.Position));
            DC.Contract.Requires(!Map[poseMean.Position]);
            DC.Contract.Requires(poseStdDev.Position.GreaterOrEqual(new Vector2()));
            DC.Contract.Requires(poseStdDev.Bearing >= 0);
            DC.Contract.Ensures(DC.Contract.ForAll(Particles, p => Map.Covers(p.Position) && !Map[p.Position]));
            DC.Contract.Ensures(DC.Contract.ForAll(Particles, p => p.Bearing.Between(0, Constants.Pi2)));

            _particleFilter.Init(Enumerable.Range(0, int.MaxValue).
                                    Select(i => new Pose(new Vector2((float)Normal.Sample(_random, poseMean.Position.X, poseStdDev.Position.X),
                                                                     (float)Normal.Sample(_random, poseMean.Position.Y, poseStdDev.Position.Y)),
                                                         (Normal.Sample(_random, poseMean.Bearing, poseStdDev.Bearing)).ToPositiveAngle())).
                                    Where(p => Map.Covers(p.Position) && !Map[p.Position]).
                                    Take(ParticlesNumber));
        }

        public void Update(Pose odometry, IEnumerable<float> scan)
		{
			DC.Contract.Requires(scan != null);
			DC.Contract.Requires(scan.Any());
            DC.Contract.Requires(Particles != null);
            DC.Contract.Requires(Particles.Any());
            DC.Contract.Ensures(Particles.Count() == ParticlesNumber);
            DC.Contract.Ensures(DC.Contract.ForAll(Particles, p => !Map.Covers(p.Position) || (Map.Covers(p.Position) && !Map[p.Position])));
            DC.Contract.Ensures(DC.Contract.ForAll(Particles, p => p.Bearing.Between(0, Constants.Pi2)));

			_particleFilter.Update(odometry, scan);
		}

        public Pose CalculatePoseMean()
        {
            DC.Contract.Requires(Particles != null);
            DC.Contract.Requires(Particles.Any());
            DC.Contract.Ensures(DC.Contract.Result<Pose>().Bearing.Between(0, Constants.Pi2));

            return new Pose(new Vector2(
                (float)MomentFor(Particles.Select(v => (double)v.Position.X), statistics => statistics.Mean),
                (float)MomentFor(Particles.Select(v => (double)v.Position.Y), statistics => statistics.Mean)),
                MathHelper2.AngleMean(Particles.Select(v => v.Bearing)));
		}

        public Pose CalculatePoseStdDev()
        {
            DC.Contract.Requires(Particles != null);
            DC.Contract.Requires(Particles.Any());
            DC.Contract.Ensures(DC.Contract.Result<Pose>().Position.GreaterOrEqual(new Vector2()));
            DC.Contract.Ensures(DC.Contract.Result<Pose>().Bearing >= 0);

			return new Pose(new Vector2(
                (float)MomentFor(Particles.Select(v => (double)v.Position.X), statistics => statistics.StandardDeviation),
                (float)MomentFor(Particles.Select(v => (double)v.Position.Y), statistics => statistics.StandardDeviation)),
                MomentFor(Particles.Select(v => v.Bearing), statistics => statistics.StandardDeviation));
        }

        public IEnumerable<Pose> GetPoseCandidates()
        {
            DC.Contract.Requires(Particles != null);
            DC.Contract.Requires(Particles.Any());
            DC.Contract.Ensures(DC.Contract.ForAll(DC.Contract.Result<IEnumerable<Pose>>(),
                p => Map.Covers(p.Position) && !Map[p.Position] && p.Bearing.Between(0, Constants.Pi2)));

            var h = new PoseHistogram(Map, THETA_BIN_SIZE);
            h.Build(Particles);
            return h.Bins.Where(pb => pb.Samples.Any()).OrderByDescending(pb => pb.Samples.Count()).Select(pb => pb.CalculatePoseMean());
        }

		static double MomentFor(IEnumerable<double> data, Func<DescriptiveStatistics, double> func)
		{
            DC.Contract.Requires(data != null);
            DC.Contract.Requires(func != null);

		    return func(new DescriptiveStatistics(data));
		}
    }
}