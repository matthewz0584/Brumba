using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Diagnostics;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Services.ManifestLoaderClient.Proxy;
using System.Linq;
using System.Xml;
using Microsoft.Dss.Services.Serializer;
using Microsoft.Dss.Core;
using Microsoft.Dss.Services.MountService;
using Mrse = Microsoft.Robotics.Simulation.Engine;
using MrsePxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using MrsPxy = Microsoft.Robotics.Simulation.Proxy;
using BrSimTimer = Brumba.Simulation.SimulatedTimer;
using BrSimTimerPxy = Brumba.Simulation.SimulatedTimer.Proxy;

namespace Brumba.SimulationTester
{
    [Contract(Contract.Identifier)]
	[DisplayName("Simulation Tester")]
	[Description("Simulation Tester service (no description provided)")]
	public class SimulationTesterService : DsspServiceExposing
	{
        public const string MANIFEST_EXTENSION = "manifest.xml";
        public const string ENVIRONMENT_EXTENSION = "environ.xml";

		public const int TRIES_NUMBER = 100;
		public const float SUCCESS_THRESHOLD = 0.79f;
        public const string TESTS_PATH = "brumba/tests";
        public const float PHYSICS_TIME_STEP = 0.01f;
	    public const string RESET_SYMBOL = "@";

		[InitialStatePartner(Optional = true)]
		SimulationTesterState _state = null;
		
		[ServicePort("/SimulationTester", AllowMultipleInstances = true)]
		SimulationTesterOperations _mainPort = new SimulationTesterOperations();

        [Partner("SimEngine", Contract = MrsePxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        MrsePxy.SimulationEnginePort _simEngine = new MrsePxy.SimulationEnginePort();

        [Partner("SimTimer", Contract = BrSimTimerPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UsePartnerListEntry)]
        BrSimTimerPxy.SimulatedTimerOperations _timer = new BrSimTimerPxy.SimulatedTimerOperations();

		[Partner("Manifest loader", Contract = Microsoft.Dss.Services.ManifestLoaderClient.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		ManifestLoaderClientPort _manifestLoader = new ManifestLoaderClientPort();

        readonly List<SimulationTestFixtureInfo> _testFixtureInfos = new List<SimulationTestFixtureInfo>();
		readonly Dictionary<SimulationTestInfo, float> _testResults = new Dictionary<SimulationTestInfo, float>();

        MrsPxy.SimulationState _initialSimState;

		public event Action OnStarted = delegate { };
        public event Action<Dictionary<SimulationTestInfo, float>> OnEnded = delegate { };
		public event Action<SimulationTestFixtureInfo> OnFixtureStarted = delegate { };
		public event Action<SimulationTestInfo> OnTestStarted = delegate { };
        public event Action<SimulationTestInfo, float> OnTestEnded = delegate { };
        public event Action<SimulationTestInfo, bool> OnTestTryEnded = delegate { };

        public SimulationTesterService()
            : base((ServiceEnvironment)null)
        {
        }
		
		public SimulationTesterService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}

        //Exposes DsspServiceBase capabilities to access other services by URI
        public T ForwardTo<T>(string serviceUri) where T : IPortSet, new()
        {
	        var serviceTcpUri = new Uri(ServiceInfo.Service);
			return ServiceForwarder<T>(String.Format(@"{0}://{1}/{2}", serviceTcpUri.Scheme, serviceTcpUri.Authority, serviceUri));
        }
		
		protected override void Start()
		{
			new SimulationTesterPresenterConsole().Setup(this);

			base.Start();

			if (_state == null)
				_state = new SimulationTesterState();

		    var simTestFixturesInfoes = new FixtureInfoCreator().CollectFixtures(Assembly.GetExecutingAssembly(), false);
		    _testFixtureInfos.AddRange(simTestFixturesInfoes.Any(fi => fi.Wip)
		                                   ? simTestFixturesInfoes.Where(fi => fi.Wip)
		                                   : simTestFixturesInfoes);

            SpawnIterator(ExecuteTests);
		}

        IEnumerator<ITask> ExecuteTests()
        {
            //Turn of "Service started ..." message visible in console, LogAsServiceActivation - is internal enum(
            DssLogHandler.SetConsoleVisibleLevel(typeof(LogGroups).GetNestedType("LogAsServiceActivation", BindingFlags.NonPublic), TraceLevel.Off);
            //DssLogHandler.AddTraceSwitchMapping(typeof(LogAsServiceActivation), );
            //var servicesTsLevel = TS.Services.Level;
            //TS.Services.Level = TraceLevel.Info;
            //TS.Services.Level = servicesTsLevel;

            yield return To.Exec(SetUpSimulator);
            
            IEnumerable<Uri> servicesBeforeStart = null;
            yield return To.Exec(GetRunningServices, (IEnumerable<Uri> ss) => servicesBeforeStart = ss);

			OnStarted();
			foreach (var fixtureInfo in _testFixtureInfos)
			{
				OnFixtureStarted(fixtureInfo);

                //Restore of static objects
                yield return To.Exec(RestoreEnvironment, fixtureInfo.Name, (Func<MrsePxy.VisualEntity, bool>)(ve => !ve.State.Name.Contains(RESET_SYMBOL)), (Action<Mrse.VisualEntity>)null);

				foreach (var testInfo in fixtureInfo.TestInfos)
				{
					OnTestStarted(testInfo);

					float result = 0;
                    yield return To.Exec(ExecuteTest, (float r) => result = r, fixtureInfo, testInfo);
					_testResults.Add(testInfo, result);

					OnTestEnded(testInfo, result);
				}

                yield return To.Exec(DropServices, servicesBeforeStart);
			}
        	OnEnded(_testResults);

			LogInfo(_testResults.Aggregate("All tests are run: ", (message, test) => string.Format("{0} {1}-{2:P0}\n", message, test.Key.Name, test.Value)));

			if (_state.ToDropHostOnFinish)
				ControlPanelPort.Post(new Microsoft.Dss.Services.ControlPanel.DropProcess());
        }

        IEnumerator<ITask> ExecuteTest(Action<float> @return, SimulationTestFixtureInfo fixtureInfo, SimulationTestInfo testInfo)
        {
            LogInfo("ExecuteTest: Test execution started");
        	int successful = 0, i;
			for (i = 0; i < (testInfo.IsProbabilistic ? TRIES_NUMBER : 1); ++i)
            {
				if (HasEarlyResults(i, successful))
					break;

                //Restore only those entities that need it (@ in name)
                yield return To.Exec(RestoreEnvironment, fixtureInfo.Name, (Func<MrsePxy.VisualEntity, bool>)(ve => ve.State.Name.Contains(RESET_SYMBOL)), testInfo.Prepare);
                LogInfo("ExecuteTest: Environment restored");

                //Pause, now we can set up for starting fixture and timer
                //PauseExecution pauses physics engine (simulation does not advance), pauses simulation timer (no tick events, so all services dependent from it pause),
                //but can not pause code in Entity.Update, this code can not know that simulator was paused. So there could be some problems. For example,
                //ReferencePlatform2011Entity accelerates from actual to target speed linearly, increasing wheel velocity by 0.5 every Update.
                //It is still happening after test was started, even if simulation was paused. Workaround is to use SimulationTimerService.GetElapsedTime helper method
                // to aquire elapsed time inside Update method, see AckermanVehicleExEntity.Update as example.
                yield return To.Exec(PauseExecution, true);

                //Restart services from fixture manifest
                yield return To.Exec(StartManifest, fixtureInfo.Name);
                LogInfo("ExecuteTest: Manifest restarted");

                //Reconnect to necessary services
                if (fixtureInfo.SetUp != null)
                {
                    fixtureInfo.SetUp(this);
                    LogInfo("ExecuteTest: Fixture set up");
                }

                //Start test (it is not really started: physics simulation is paused)
                if (testInfo.Start != null)
                {
                    yield return To.Exec(testInfo.Start);
                    LogInfo("ExecuteTest: Test try started");
                }

                //Start sim timer (it is not really started: simulation timer is paused)
                var subscribeRq = _timer.Subscribe(testInfo.EstimatedTime);
                yield return To.Exec(subscribeRq.ResponsePort);

                //Unpause all, timer and test will start soon after
                yield return To.Exec(PauseExecution, false);

                //Wait for estimated time
                var dt = 0.0;
                yield return (subscribeRq.NotificationPort as BrSimTimerPxy.SimulatedTimerOperations).P4.Receive(u => dt = u.Body.Delta);
                subscribeRq.NotificationShutdownPort.Post(new Shutdown());
                LogInfo("ExecuteTest: Test estimated time elapsed");

                //Pause all, so that sim state will not differ from state from services due to delays between queries
                yield return To.Exec(PauseExecution, true);

                //Get testee state from simulator
                MrsPxy.SimulationState simState = null;
                yield return _simEngine.Get().Choice(st => simState = st, LogError);
                LogInfo("ExecuteTest: Simulation engine state acquired");

                //Deserialize it
                IEnumerable<MrsePxy.VisualEntity> testeeEntitiesPxies = null;
                yield return To.Exec(DeserializaTopLevelEntityProxies,
                            (IEnumerable<MrsePxy.VisualEntity> ens) => testeeEntitiesPxies = ens, simState,
                            (Func<XmlElement, bool>)
                            (xe => xe.SelectSingleNode(@"/*[local-name()='State']/*[local-name()='Name']/text()").InnerText.Contains(RESET_SYMBOL)));
                LogInfo("ExecuteTest: Testee entities deserialized");

                //Check test's result
                var testSucceed = false;
                if (testInfo.Test != null)
                {
                    yield return To.Exec(testInfo.Test, b => testSucceed = b, testeeEntitiesPxies, dt);
                    LogInfo("ExecuteTest: Test results assessed");
                }

                OnTestTryEnded(testInfo, testSucceed);

                if (testSucceed) ++successful;

	            //Drop services that need to be restarted
				yield return To.Exec(DropResettableServices);
                LogInfo("ExecuteTest: Test's services dropped");
            }

            @return((float)successful / i);
        }

        IEnumerator<ITask> SetUpSimulator()
        {
            yield return To.Exec(_simEngine.UpdatePhysicsTimeStep(PHYSICS_TIME_STEP));
            //yield return To.Exec(_simEngine.UpdateSimulatorConfiguration(new EngPxy.SimulatorConfiguration { Headless = true }));

            yield return Arbiter.Choice(_simEngine.Get(), s => _initialSimState = s, LogError);

            _initialSimState.RenderMode = _state.ToRender ? MrsPxy.RenderMode.Full : MrsPxy.RenderMode.None;
            yield return To.Exec(_simEngine.Replace(_initialSimState));
        }

        IEnumerator<ITask> DropServices(IEnumerable<Uri> except)
        {
            IEnumerable<Uri> runningServices = null;
            yield return To.Exec(GetRunningServices, (IEnumerable<Uri> ss) => runningServices = ss);

            foreach (var uri in runningServices.Where(uri => !except.Contains(uri)).Distinct(new ServiceUriComparer()))
                yield return To.Exec(DropService, uri);
        }

        IEnumerator<ITask> StartManifest(string manifest)
        {
            yield return To.Exec(_manifestLoader.Insert(new InsertRequest
                {
                    Manifest = new UriBuilder(ServiceInfo.HttpServiceAlias.Scheme, ServiceInfo.HttpServiceAlias.Host, ServiceInfo.HttpServiceAlias.Port,
                                              String.Format(@"{0}/{1}/{2}.{3}", ServicePaths.MountPoint, TESTS_PATH, manifest, MANIFEST_EXTENSION)).ToString()
                }));
        }

        IEnumerator<ITask> DropResettableServices()
        {
            IEnumerable<Uri> runningServices = null;
            yield return To.Exec(GetRunningServices, (IEnumerable<Uri> ss) => runningServices = ss);

            foreach (var uri in runningServices.Where(uri => uri.LocalPath.Contains(RESET_SYMBOL)))
                yield return To.Exec(DropService, uri);
        }

        IEnumerator<ITask> GetRunningServices(Action<IEnumerable<Uri>> @return)
        {
            var directoryGetRq = new Microsoft.Dss.Services.Directory.Get();
            DirectoryPort.Post(directoryGetRq);
            Microsoft.Dss.Services.Directory.GetResponseType dirState = null;
            yield return directoryGetRq.ResponsePort.Receive(dSt => dirState = dSt);

            @return(dirState.RecordList.Select(si => si.HttpServiceAlias).ToList());
        }

        IEnumerator<ITask> DropService(Uri uri)
        {
            var serviceDropPort = ServiceForwarder<PortSet<DsspDefaultLookup, DsspDefaultDrop>>(uri);
            var dsspDefaultDropRq = new DsspDefaultDrop();
            serviceDropPort.Post(dsspDefaultDropRq);
            yield return dsspDefaultDropRq.ResponsePort.Choice(
                dropped => { },
                failed => LogError(String.Format("Service {0} can not be dropped", uri)));
        }

        IEnumerator<ITask> PauseExecution(bool pause)
        {
            _initialSimState.Pause = pause;
            yield return To.Exec(_simEngine.Replace(_initialSimState));
            yield return To.Exec(_timer.Pause(pause));
        }

        IEnumerator<ITask> RestoreEnvironment(string environmentXmlFile, Func<MrsePxy.VisualEntity, bool> resetFilter, Action<Mrse.VisualEntity> prepareEntityForReset)
        {
            MrsPxy.SimulationState simState = null;
            yield return Arbiter.Choice(_simEngine.Get(), st => simState = st, LogError);

            var renderMode = simState.RenderMode;
            simState.Pause = true;
            yield return To.Exec(_simEngine.Replace(simState));

            IEnumerable<MrsePxy.VisualEntity> entityPxies = null;
            yield return To.Exec(DeserializaTopLevelEntityProxies, (IEnumerable<MrsePxy.VisualEntity> ePxies) => entityPxies = ePxies, simState, (Func<XmlElement, bool>)null);
            foreach (var entityPxy in entityPxies.Where(resetFilter ?? (pxy => true)).Where(pxy => pxy.ParentJoint == null).Union(entityPxies.Where(pxy => pxy.State.Name == "timer")))
                yield return Arbiter.Choice(_simEngine.DeleteSimulationEntity(entityPxy), deleted => {}, failed => {});

            simState.Pause = false;
            simState.RenderMode = renderMode;
            yield return To.Exec(_simEngine.Replace(simState));

            var get = new DsspDefaultGet();
            ServiceForwarder<MountServiceOperations>(String.Format(@"{0}/{1}/{2}.{3}", ServicePaths.MountPoint, TESTS_PATH, environmentXmlFile, ENVIRONMENT_EXTENSION)).Post(get);
            yield return Arbiter.Choice(get.ResponsePort, LogError, success => simState = (MrsPxy.SimulationState)success);

            IEnumerable<Microsoft.Robotics.Simulation.Engine.VisualEntity> entities = null;
            yield return To.Exec(DeserializeTopLevelEntities, (IEnumerable<Mrse.VisualEntity> ens) => entities = ens, simState);
            foreach (var entity in entities.Where(entity => resetFilter((MrsePxy.VisualEntity)DssTypeHelper.TransformToProxy(entity))))
			{
				if (prepareEntityForReset != null)
					prepareEntityForReset(entity);
                var insRequest = new Mrse.InsertSimulationEntity(entity);
                Mrse.SimulationEngine.GlobalInstancePort.Post(insRequest);
				yield return To.Exec(insRequest.ResponsePort);
			}

            var timerInsRequest = new Mrse.InsertSimulationEntity(new BrSimTimer.TimerEntity("timer"));
            Mrse.SimulationEngine.GlobalInstancePort.Post(timerInsRequest);
            yield return To.Exec(timerInsRequest.ResponsePort);
        }

        IEnumerator<ITask> DeserializaTopLevelEntityProxies(Action<IEnumerable<MrsePxy.VisualEntity>> @return, MrsPxy.SimulationState simState, Func<XmlElement, bool> filter)
		{
			var entities = new List<MrsePxy.VisualEntity>();
			foreach (var entityNode in simState.SerializedEntities.XmlNodes.Cast<XmlElement>().Where(filter ?? (xe => true)))
			{
                MrsePxy.VisualEntity entityPxy = null;
                yield return To.Exec(DeserializeEntityPxyFromXml, (MrsePxy.VisualEntity e) => entityPxy = e, entityNode);
				if (entityPxy.State.Name == "MainCamera")
					continue;
				entities.Add(entityPxy);
			}
			@return(entities);
		}

        IEnumerator<ITask> DeserializeTopLevelEntities(Action<IEnumerable<Mrse.VisualEntity>> @return, MrsPxy.SimulationState simState)
        {
            IEnumerable<MrsePxy.VisualEntity> entitiesFlatPxies = null;
            yield return To.Exec(DeserializaTopLevelEntityProxies, (IEnumerable<MrsePxy.VisualEntity> es) => entitiesFlatPxies = es, simState, (Func<XmlElement, bool>)null);

            var entitiesFlat = entitiesFlatPxies.Select(ePxy => (Mrse.VisualEntity)DssTypeHelper.TransformFromProxy(ePxy));

            var entitiesTop = new List<Mrse.VisualEntity>();
            while (entitiesFlat.Any())
            {
                entitiesTop.Add(entitiesFlat.First());
                yield return To.Exec(ReuniteEntity, (IEnumerable<Mrse.VisualEntity> withoutChildren) => entitiesFlat = withoutChildren.ToList(), entitiesFlat.First(), entitiesFlat.Skip(1));
            }
            @return(entitiesTop);
        }

        IEnumerator<ITask> ReuniteEntity(Action<IEnumerable<Mrse.VisualEntity>> @return, Mrse.VisualEntity parent, IEnumerable<Mrse.VisualEntity> entitiesFlat)
        {
            if (parent.ChildCount != 0)
	            for (var i = 0; i < parent.ChildCount; ++i)
		        {
			        parent.InsertEntity(entitiesFlat.First());
                    yield return To.Exec(ReuniteEntity, (IEnumerable<Mrse.VisualEntity> withoutChildren) => entitiesFlat = withoutChildren, entitiesFlat.First(), entitiesFlat.Skip(1));
				}
            @return(entitiesFlat);
        }

        IEnumerator<ITask> DeserializeEntityPxyFromXml(Action<MrsePxy.VisualEntity> @return, XmlElement entityNode)
        {
            var desRequest = new Deserialize(new XmlNodeReader(entityNode));
            SerializerPort.Post(desRequest);
            DeserializeResult desEntity = null;
            yield return Arbiter.Choice(desRequest.ResultPort, v => desEntity = v, LogError);
            @return((MrsePxy.VisualEntity)desEntity.Instance);
        }

        static bool HasEarlyResults(int i, int successful)
		{
			return i == TRIES_NUMBER / 10 && ((float)successful / i > SUCCESS_THRESHOLD || (float)successful / i < 1 - SUCCESS_THRESHOLD);
		}
	}
}


