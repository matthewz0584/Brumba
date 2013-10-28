using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Services.ManifestLoaderClient.Proxy;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using SimPxy = Microsoft.Robotics.Simulation.Proxy;
using Microsoft.Robotics.Simulation.Engine;
using StPxy = Brumba.Simulation.SimulatedTimer.Proxy;
using System.Linq;
using System.Xml;
using Microsoft.Dss.Services.Serializer;
using Microsoft.Dss.Core;
using Microsoft.Dss.Services.MountService;
using Brumba.Simulation.SimulatedTimer;

namespace Brumba.Simulation.SimulationTester
{
    [Contract(Contract.Identifier)]
	[DisplayName("Simulation Tester")]
	[Description("Simulation Tester service (no description provided)")]
	public class SimulationTesterService : DsspServiceBase
	{
		public const int TRIES_NUMBER = 100;
		public const float SUCCESS_THRESHOLD = 0.79f;
		public const SimPxy.RenderMode RENDER_MODE = SimPxy.RenderMode.None;
		//public const SimPxy.RenderMode RENDER_MODE = SimPxy.RenderMode.Full;
        public const string TESTS_PATH = "brumba/tests";

		[ServiceState]
		SimulationTesterState _state = new SimulationTesterState();
		
		[ServicePort("/SimulationTester", AllowMultipleInstances = true)]
		SimulationTesterOperations _mainPort = new SimulationTesterOperations();

        [Partner("SimEngine", Contract = Microsoft.Robotics.Simulation.Engine.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        EngPxy.SimulationEnginePort _simEngine = new EngPxy.SimulationEnginePort();

        [Partner("SimTimer", Contract = StPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UsePartnerListEntry)]
        StPxy.SimulatedTimerOperations _timer = new StPxy.SimulatedTimerOperations();

		[Partner("Manifest loader", Contract = Microsoft.Dss.Services.ManifestLoaderClient.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		ManifestLoaderClientPort _manifestLoader = new ManifestLoaderClientPort();

        readonly List<ISimulationTestFixture> _testFixtures = new List<ISimulationTestFixture>();
		readonly Dictionary<ISimulationTest, float> _testResults = new Dictionary<ISimulationTest, float>();

		public event Action OnStarted = delegate { };
		public event Action<Dictionary<ISimulationTest, float>> OnEnded = delegate { };
		public event Action<ISimulationTestFixture> OnFixtureStarted = delegate { };
		public event Action<ISimulationTest> OnTestStarted = delegate { };
		public event Action<ISimulationTest, float> OnTestEnded = delegate { };
		public event Action<ISimulationTest, bool> OnTestTryEnded = delegate { };
		
		public SimulationTesterService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}

        //Exposes DsspServiceBase capabilities to access other services by URI
        public T ForwardTo<T>(string serviceUri) where T : IPortSet, new()
        {
            return ServiceForwarder<T>(String.Format(@"{0}://{1}/{2}", ServiceInfo.HttpServiceAlias.Scheme, ServiceInfo.HttpServiceAlias.Authority, serviceUri));
        }
		
		protected override void Start()
		{
			new SimulationTesterPresenterConsole().Setup(this);

			base.Start();

            _testFixtures.AddRange(GatherTestFixtures());

            SpawnIterator(ExecuteTests);
		}

        IEnumerable<ISimulationTestFixture> GatherTestFixtures()
        {
            var wipFixtureTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttributes(false).Any(a => a is SimulationTestFixtureAttribute && !(a as SimulationTestFixtureAttribute).Ignore && (a as SimulationTestFixtureAttribute).Wip));
            var allFixtureTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttributes(false).Any(a => a is SimulationTestFixtureAttribute && !(a as SimulationTestFixtureAttribute).Ignore));
            var fixturesToCreate = wipFixtureTypes.Any() ? wipFixtureTypes : allFixtureTypes;
            var sf = new ServiceForwarder(this);
            return fixturesToCreate.Select(ft => Activator.CreateInstance(ft, new object[] { sf })).Cast<ISimulationTestFixture>();
        }

        IEnumerator<ITask> ExecuteTests()
        {
            yield return To.Exec(SetUpSimulator);

			OnStarted();
			foreach (var fixture in _testFixtures)
			{
				OnFixtureStarted(fixture);

                //Start fixture manifest
                yield return To.Exec(StartFixtureManifest, fixture);
                
                //Connect to necessary services
                fixture.SetUpServicePorts();

                //Full restore: static and dynamic objects
                yield return To.Exec(RestoreEnvironment, fixture.EnvironmentXmlFile, new Func<EngPxy.VisualEntity, bool>(es => true), new Action<VisualEntity>(e => {}));

				foreach (var test in fixture.Tests)
				{
					OnTestStarted(test);

					float result = 0;
					yield return To.Exec(ExecuteTest, (float r) => result = r, test);
					_testResults.Add(test, result);

					OnTestEnded(test, result);
				}
			}
        	OnEnded(_testResults);

			LogInfo(_testResults.Aggregate("All tests are run: ", (message, test) => string.Format("{0} {1}-{2:P0}\n", message, test.Key.GetType().Name, test.Value)));
        }

        IEnumerator<ITask> StartFixtureManifest(ISimulationTestFixture fixture)
        {
            yield return To.Exec(
                _manifestLoader.Insert(new InsertRequest
                    {
                        Manifest = String.Format(@"{0}://{1}{2}/{3}/{4}.{5}",
                                                 ServiceInfo.HttpServiceAlias.Scheme,
                                                 ServiceInfo.HttpServiceAlias.Authority,
                                                 ServicePaths.MountPoint,
                                                 TESTS_PATH,
                                                 fixture.EnvironmentXmlFile,
                                                 SimulationTestFixture.MANIFEST_EXTENSION)
                    }));
        }

        IEnumerator<ITask> SetUpSimulator()
        {
            yield return To.Exec(_simEngine.UpdatePhysicsTimeStep(0.01f));
            //yield return To.Exec(_simEngine.UpdateSimulatorConfiguration(new EngPxy.SimulatorConfiguration { Headless = true }));

            SimPxy.SimulationState simState = null;
            yield return Arbiter.Choice(_simEngine.Get(), s => simState = s, LogError);

			simState.RenderMode = RENDER_MODE;
            yield return To.Exec(_simEngine.Replace(simState));
        }

        IEnumerator<ITask> ExecuteTest(Action<float> @return, ISimulationTest test)
        {
        	int successful = 0, i;
			for (i = 0; i < (test.IsProbabilistic ? TRIES_NUMBER : 1); ++i)
            {
				if (HasEarlyResults(i, successful))
					break;

                yield return To.Exec(RestoreEnvironment, test.Fixture.EnvironmentXmlFile, (Func<EngPxy.VisualEntity, bool>)test.NeedResetOnEachTry, (Action<VisualEntity>)test.PrepareForReset);

                yield return To.Exec(test.Start);

                var testSucceed = false;
                
                var elapsedTime = 0.0;                
                while (!testSucceed && elapsedTime <= test.EstimatedTime * 2)
                {
                    SimPxy.SimulationState simState = null;
                    yield return Arbiter.Choice(_simEngine.Get(), st => simState = st, LogError);

					IEnumerable<EngPxy.VisualEntity> simStateEntities = null;
					yield return To.Exec(DeserializaTopLevelEntityProxies, (IEnumerable<EngPxy.VisualEntity> ens) => simStateEntities = ens, simState);
                    yield return To.Exec(test.AssessProgress, (bool b) => testSucceed = b, simStateEntities, elapsedTime);

                    yield return Arbiter.Choice(_timer.Get(), s => elapsedTime = s.ElapsedTime, LogError);

                    if (!testSucceed)
                        yield return To.Exec(TimeoutPort(50));
                }
            	OnTestTryEnded(test, testSucceed);

                if (testSucceed) ++successful;
            }

            @return((float)successful / i);
        }

        IEnumerator<ITask> RestoreEnvironment(string environmentXmlFile, Func<EngPxy.VisualEntity, bool> resetFilter, Action<VisualEntity> prepareEntityForReset)
        {
            SimPxy.SimulationState simState = null;
            yield return Arbiter.Choice(_simEngine.Get(), st => simState = st, LogError);

            var renderMode = simState.RenderMode;
            simState.Pause = true;
            yield return To.Exec(_simEngine.Replace(simState));

            IEnumerable<EngPxy.VisualEntity> entityPxies = null;
			yield return To.Exec(DeserializaTopLevelEntityProxies, (IEnumerable<EngPxy.VisualEntity> ePxies) => entityPxies = ePxies, simState);
            foreach (var entityPxy in entityPxies.Where(resetFilter).Where(pxy => pxy.ParentJoint == null).Union(entityPxies.Where(pxy => pxy.State.Name == "timer")))
                yield return To.Exec(_simEngine.DeleteSimulationEntity(entityPxy));

            simState.Pause = false;
            simState.RenderMode = renderMode;
            yield return To.Exec(_simEngine.Replace(simState));

            var get = new DsspDefaultGet();
            ServiceForwarder<MountServiceOperations>(String.Format(@"{0}/{1}/{2}.{3}", ServicePaths.MountPoint, TESTS_PATH, environmentXmlFile, SimulationTestFixture.ENVIRONMENT_EXTENSION)).Post(get);
            yield return Arbiter.Choice(get.ResponsePort, LogError, success => simState = (Microsoft.Robotics.Simulation.Proxy.SimulationState)success);

            IEnumerable<VisualEntity> entities = null;
            yield return To.Exec(DeserializeTopLevelEntities, (IEnumerable<VisualEntity> ens) => entities = ens, simState);
            foreach (var entity in entities.Where(entity => resetFilter((EngPxy.VisualEntity)DssTypeHelper.TransformToProxy(entity))))
			{
                prepareEntityForReset(entity);
			    var insRequest = new InsertSimulationEntity(entity);
				SimulationEngine.GlobalInstancePort.Post(insRequest);
				yield return To.Exec(insRequest.ResponsePort);
			}

			SimulationEngine.GlobalInstancePort.Insert(new TimerEntity("timer"));
        }

		IEnumerator<ITask> DeserializaTopLevelEntityProxies(Action<IEnumerable<EngPxy.VisualEntity>> @return, SimPxy.SimulationState simState)
		{
			var entities = new List<EngPxy.VisualEntity>();
			foreach (var entityNode in simState.SerializedEntities.XmlNodes.Cast<XmlElement>())
			{
				EngPxy.VisualEntity entityPxy = null;
				yield return To.Exec(DeserializeEntityFromXml, (EngPxy.VisualEntity e) => entityPxy = e, entityNode);
				if (entityPxy.State.Name == "MainCamera")
					continue;
				entities.Add(entityPxy);
			}
			@return(entities);
		}

        IEnumerator<ITask> DeserializeTopLevelEntities(Action<IEnumerable<VisualEntity>> @return, SimPxy.SimulationState simState)
        {
			IEnumerable<EngPxy.VisualEntity> entitiesFlatPxies = null;
			yield return To.Exec(DeserializaTopLevelEntityProxies, (IEnumerable<EngPxy.VisualEntity> es) => entitiesFlatPxies = es, simState);

			var entitiesFlat = entitiesFlatPxies.Select(ePxy => (VisualEntity)DssTypeHelper.TransformFromProxy(ePxy));

            var entitiesTop = new List<VisualEntity>();
            while (entitiesFlat.Any())
            {
                entitiesTop.Add(entitiesFlat.First());
                yield return To.Exec(ReuniteEntity, (IEnumerable<VisualEntity> withoutChildren) => entitiesFlat = withoutChildren.ToList(), entitiesFlat.First(), entitiesFlat.Skip(1));
            }
            @return(entitiesTop);
        }

        IEnumerator<ITask> ReuniteEntity(Action<IEnumerable<VisualEntity>> @return, VisualEntity parent, IEnumerable<VisualEntity> entitiesFlat)
        {
            if (parent.ChildCount != 0)
	            for (var i = 0; i < parent.ChildCount; ++i)
		        {
			        parent.InsertEntity(entitiesFlat.First());
				    yield return To.Exec(ReuniteEntity, (IEnumerable<VisualEntity> withoutChildren) => entitiesFlat = withoutChildren, entitiesFlat.First(), entitiesFlat.Skip(1));
				}
            @return(entitiesFlat);
        }

        IEnumerator<ITask> DeserializeEntityFromXml(Action<EngPxy.VisualEntity> @return, XmlElement entityNode)
        {
            var desRequest = new Deserialize(new XmlNodeReader(entityNode));
            SerializerPort.Post(desRequest);
            DeserializeResult desEntity = null;
            yield return Arbiter.Choice(desRequest.ResultPort, v => desEntity = v, LogError);
            @return((EngPxy.VisualEntity)desEntity.Instance);
        }

        static bool HasEarlyResults(int i, int successful)
		{
			return i == TRIES_NUMBER / 10 && ((float)successful / i > SUCCESS_THRESHOLD || (float)successful / i < 1 - SUCCESS_THRESHOLD);
		}
	}
}


