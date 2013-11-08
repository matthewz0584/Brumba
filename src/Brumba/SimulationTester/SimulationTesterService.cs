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
        public const string MANIFEST_EXTENSION = "manifest.xml";
        public const string ENVIRONMENT_EXTENSION = "environ.xml";

		public const int TRIES_NUMBER = 100;
		public const float SUCCESS_THRESHOLD = 0.79f;
		//public const SimPxy.RenderMode RENDER_MODE = SimPxy.RenderMode.None;
		public const SimPxy.RenderMode RENDER_MODE = SimPxy.RenderMode.Full;
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

        readonly List<SimulationTestFixtureInfo> _testFixtureInfos = new List<SimulationTestFixtureInfo>();
		readonly Dictionary<ISimulationTest, float> _testResults = new Dictionary<ISimulationTest, float>();

		public event Action OnStarted = delegate { };
		public event Action<Dictionary<ISimulationTest, float>> OnEnded = delegate { };
		public event Action<SimulationTestFixtureInfo> OnFixtureStarted = delegate { };
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

            _testFixtureInfos.AddRange(GatherTestFixtures());

            SpawnIterator(ExecuteTests);
		}

        IEnumerable<SimulationTestFixtureInfo> GatherTestFixtures()
        {
            var wipFixtureTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttributes(false).Any(a => a is SimTestFixtureAttribute && !(a as SimTestFixtureAttribute).Ignore && (a as SimTestFixtureAttribute).Wip));
            var allFixtureTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttributes(false).Any(a => a is SimTestFixtureAttribute && !(a as SimTestFixtureAttribute).Ignore));
            var fixturesToCreate = wipFixtureTypes.Any() ? wipFixtureTypes : allFixtureTypes;

            return fixturesToCreate.Select(CreateFixtureInfo);
        }

        SimulationTestFixtureInfo CreateFixtureInfo(Type fixtureType)
        {
            var fixtureInfo = new SimulationTestFixtureInfo();

            fixtureInfo.Fixture = Activator.CreateInstance(fixtureType);
            
            fixtureInfo.EnvironmentXmlFile =
                fixtureType.GetCustomAttributes(false).OfType<SimTestFixtureAttribute>().Single().EnvironmentFile;

            fixtureInfo.SetUp =
                sf =>
                fixtureType.GetMethods().Single(mi => mi.GetCustomAttributes(false).Any(a => a is SimSetUpAttribute))
                           .Invoke(fixtureInfo.Fixture, new object[] {sf});

            var testsToCreate = fixtureType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                           .Where(t => t.GetCustomAttributes(false).Any(a => a is SimTestAttribute));
            foreach (var testType in testsToCreate)
            {
                var test = Activator.CreateInstance(testType) as ISimulationTest;
                test.Fixture = fixtureInfo.Fixture;
                fixtureInfo.Tests.Add(test);
            }
            return fixtureInfo;
        }

        IEnumerator<ITask> ExecuteTests()
        {
            yield return To.Exec(SetUpSimulator);

			OnStarted();
			foreach (var fixtureInfo in _testFixtureInfos)
			{
				OnFixtureStarted(fixtureInfo);

                //Start fixtureInfo manifest
                yield return To.Exec(StartManifest, fixtureInfo.EnvironmentXmlFile);
                
                //Connect to necessary services
                fixtureInfo.SetUp(new ServiceForwarder(this));

                //Full restore: static and dynamic objects
                yield return To.Exec(RestoreEnvironment, fixtureInfo.EnvironmentXmlFile, new Func<EngPxy.VisualEntity, bool>(es => true), new Action<VisualEntity>(e => {}));

				foreach (var test in fixtureInfo.Tests)
				{
					OnTestStarted(test);

					float result = 0;
                    yield return To.Exec(ExecuteTest, (float r) => result = r, fixtureInfo, test);
					_testResults.Add(test, result);

					OnTestEnded(test, result);
				}
			}
        	OnEnded(_testResults);

			LogInfo(_testResults.Aggregate("All tests are run: ", (message, test) => string.Format("{0} {1}-{2:P0}\n", message, test.Key.GetType().Name, test.Value)));

			//_simEngine.DsspDefaultDrop();
        }

        IEnumerator<ITask> StartManifest(string manifest)
        {
            yield return To.Exec(
                _manifestLoader.Insert(new InsertRequest
                    {
                        Manifest = String.Format(@"{0}://{1}{2}/{3}/{4}.{5}",
                                                 ServiceInfo.HttpServiceAlias.Scheme,
                                                 ServiceInfo.HttpServiceAlias.Authority,
                                                 ServicePaths.MountPoint,
                                                 TESTS_PATH,
                                                 manifest,
                                                 MANIFEST_EXTENSION)
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

        IEnumerator<ITask> ExecuteTest(Action<float> @return, SimulationTestFixtureInfo fixtureInfo, ISimulationTest test)
        {
        	int successful = 0, i;
			for (i = 0; i < (test.IsProbabilistic ? TRIES_NUMBER : 1); ++i)
            {
				if (HasEarlyResults(i, successful))
					break;

                yield return To.Exec(RestoreEnvironment, fixtureInfo.EnvironmentXmlFile, (Func<EngPxy.VisualEntity, bool>)test.NeedResetOnEachTry, (Action<VisualEntity>)test.PrepareForReset);

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
            ServiceForwarder<MountServiceOperations>(String.Format(@"{0}/{1}/{2}.{3}", ServicePaths.MountPoint, TESTS_PATH, environmentXmlFile, ENVIRONMENT_EXTENSION)).Post(get);
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


