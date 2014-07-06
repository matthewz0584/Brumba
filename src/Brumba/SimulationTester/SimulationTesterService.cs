using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Brumba.DsspUtils;
using Brumba.Simulation.SimulatedTimer;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Diagnostics;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Services.ManifestLoaderClient.Proxy;
using System.Linq;
using System.Xml;
using Microsoft.Dss.Core;
using Microsoft.Dss.Services.MountService;
using Microsoft.Robotics.PhysicalModel;
using W3C.Soap;
using Mrse = Microsoft.Robotics.Simulation.Engine;
using MrsePxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using MrsPxy = Microsoft.Robotics.Simulation.Proxy;
using BrTimerPxy = Brumba.Entities.Timer.Proxy;

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
	    public const string RESET_SYMBOL = "@";

		[InitialStatePartner(Optional = true)]
		SimulationTesterState _state = null;
		
		[ServicePort("/SimulationTester", AllowMultipleInstances = true)]
		SimulationTesterOperations _mainPort = new SimulationTesterOperations();

        [Partner("SimEngine", Contract = MrsePxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        MrsePxy.SimulationEnginePort _simEngine = new MrsePxy.SimulationEnginePort();

        [Partner("Timer", Contract = BrTimerPxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		BrTimerPxy.TimerOperations _timer = new BrTimerPxy.TimerOperations();

		[Partner("Manifest loader", Contract = Microsoft.Dss.Services.ManifestLoaderClient.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		ManifestLoaderClientPort _manifestLoader = new ManifestLoaderClientPort();

        EntityDeserializer _entityDeserializer;

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
            _entityDeserializer = new EntityDeserializer(SerializerPort, LogError);
		}

	    public BrTimerPxy.TimerOperations Timer
	    {
		    get { return _timer; }
	    }

	    //Exposes DsspServiceBase capabilities to access other services by URI
        public T ForwardTo<T>(string serviceUri) where T : IPortSet, new()
        {
	        var serviceTcpUri = new Uri(ServiceInfo.Service);
			return ServiceForwarder<T>(String.Format(@"{0}://{1}/{2}", serviceTcpUri.Scheme, serviceTcpUri.Authority, serviceUri));
        }

        public IEnumerator<ITask> GetTesteeEntityProxies(Action<IEnumerable<MrsePxy.VisualEntity>> @return)
        {
            yield return To.Exec(GetAndDeserializeEntityPxies, @return, (Func<XmlElement, bool>)IsTesteeXmlNode);
        }
		
		protected override void Start()
		{
			new SimulationTesterPresenterConsole().Setup(this);

			base.Start();

			if (_state == null)
				_state = new SimulationTesterState();

		    var simTestFixturesInfoes = new FixtureInfoCreator().CollectFixtures(Assembly.GetExecutingAssembly(), false);

            SpawnIterator(
                simTestFixturesInfoes.Any(fi => fi.Wip) ? simTestFixturesInfoes.Where(fi => fi.Wip).ToList() : simTestFixturesInfoes,
                RunFixtures);
		}

        IEnumerator<ITask> RunFixtures(List<SimulationTestFixtureInfo> testFixtureInfos)
        {
            //Turn of "Service started ..." message visible in console, LogAsServiceActivation - is internal enum(
            DssLogHandler.SetConsoleVisibleLevel(typeof(LogGroups).GetNestedType("LogAsServiceActivation", BindingFlags.NonPublic), TraceLevel.Off);
            //DssLogHandler.AddTraceSwitchMapping(typeof(LogAsServiceActivation), );
            //var servicesTsLevel = TS.Services.Level;
            //TS.Services.Level = TraceLevel.Info;
            //TS.Services.Level = servicesTsLevel;

			yield return Arbiter.Choice(_simEngine.Get(), s => _initialSimState = s, LogError);
			_initialSimState.RenderMode = _state.ToRender ? MrsPxy.RenderMode.Full : MrsPxy.RenderMode.None;
            yield return To.Exec(_simEngine.Replace(_initialSimState));

            IEnumerable<Uri> servicesBeforeStart = null;
            yield return To.Exec(GetRunningServices, (IEnumerable<Uri> ss) => servicesBeforeStart = ss);

			OnStarted();

            foreach (var fixtureInfo in testFixtureInfos)
                yield return To.Exec(RunFixture, fixtureInfo, servicesBeforeStart);

            OnEnded(_testResults);

			LogInfo(_testResults.Aggregate("All tests are run: ", (message, test) => string.Format("{0} {1}-{2:P0}\n", message, test.Key.Name, test.Value)));

			if (_state.ToDropHostOnFinish)
				ControlPanelPort.Post(new Microsoft.Dss.Services.ControlPanel.DropProcess());
        }

        IEnumerator<ITask> RunFixture(SimulationTestFixtureInfo testFixtureInfo, IEnumerable<Uri> servicesBeforeStart)
        {
            LogInfo(SimulationTesterLogCategory.FixtureStarted, testFixtureInfo.Name);
            OnFixtureStarted(testFixtureInfo);

			yield return To.Exec(SetUpSimulator, testFixtureInfo);

            //Restore static objects
            yield return To.Exec(RestoreEnvironment, testFixtureInfo.Name, (Func<Mrse.VisualEntity, bool>)(ve => true), (Action<Mrse.VisualEntity>)null);
            LogInfo(SimulationTesterLogCategory.FixtureStaticEnvironmentRestored, testFixtureInfo.Name);

            foreach (var testInfo in testFixtureInfo.TestInfos)
            {
                OnTestStarted(testInfo);

                float result = 0;
                yield return To.Exec(RunTest, (float r) => result = r, testFixtureInfo, testInfo);
                _testResults.Add(testInfo, result);

                OnTestEnded(testInfo, result);
            }

			yield return To.Exec(DropServices, (Func<Uri, bool>)(uri => !servicesBeforeStart.Contains(uri)));
            LogInfo(SimulationTesterLogCategory.FixtureServicesDropped, testFixtureInfo.Name);
            LogInfo(SimulationTesterLogCategory.FixtureFinished, testFixtureInfo.Name);
        }

        IEnumerator<ITask> RunTest(Action<float> @return, SimulationTestFixtureInfo fixtureInfo, SimulationTestInfo testInfo)
        {
            LogInfo(SimulationTesterLogCategory.TestStarted, fixtureInfo.Name, testInfo.Name);
        	int successful = 0, i;
			for (i = 0; i < (testInfo.IsProbabilistic ? TRIES_NUMBER : 1); ++i)
            {
				if (HasEarlyResults(i, successful))
					break;

                //Restore only those entities that need it (@ in name)
                yield return To.Exec(RestoreEnvironment, fixtureInfo.Name, (Func<Mrse.VisualEntity, bool>)(ve => ve.State.Name.Contains(RESET_SYMBOL)), testInfo.Prepare);
                LogInfo(SimulationTesterLogCategory.TestEnvironmentRestored, fixtureInfo.Name, testInfo.Name, i);

                //Pause, now we can set up for starting fixture and timer
                //PauseExecution pauses physics engine (simulation does not advance), pauses simulation timer (no tick events, so all services dependent from it pause),
                //but can not pause code in Entity.Update, this code can not know that simulator was paused. So there could be some problems. For example,
                //ReferencePlatform2011Entity accelerates from actual to target speed linearly, increasing wheel velocity by 0.5 every Update.
                //It is still happening after test was started, even if simulation was paused. Workaround is to use SimulationTimerService.GetElapsedTime helper method
                //to aquire elapsed time inside Update method, see AckermanVehicleExEntity.Update as example.
                yield return To.Exec(PauseExecution, true);

                //Restart services from fixture manifest
                yield return To.Exec(StartManifest, fixtureInfo.Name);
                LogInfo(SimulationTesterLogCategory.TestManifestRestarted, fixtureInfo.Name, testInfo.Name, i);

                //Reconnect to necessary services
                if (fixtureInfo.SetUp != null)
                {
                    fixtureInfo.SetUp(this);
                    LogInfo(SimulationTesterLogCategory.TestFixtureSetUp, fixtureInfo.Name, testInfo.Name, i);
                }

                //Start test (it is not really started: physics simulation is paused)
                if (testInfo.Start != null)
                {
                    yield return To.Exec(testInfo.Start);
                    LogInfo(SimulationTesterLogCategory.TestTryStarted, fixtureInfo.Name, testInfo.Name, i);
                }

                //Start sim timer (it is not really started: simulation timer is paused)
                var subscribeRq = Timer.Subscribe(testInfo.EstimatedTime);
                yield return To.Exec(subscribeRq.ResponsePort);

                //Unpause all, timer and test will start soon after
                yield return To.Exec(PauseExecution, false);

                //Wait for estimated time
                var dt = 0.0;
                yield return (subscribeRq.NotificationPort as BrTimerPxy.TimerOperations).P4.Receive(u => dt = u.Body.Delta);
                subscribeRq.NotificationShutdownPort.Post(new Shutdown());
                //yield return TimeoutPort(5000).Receive();
                LogInfo(SimulationTesterLogCategory.TestEstimatedTimeElapsed, fixtureInfo.Name, testInfo.Name, i, dt);

                //Pause all, so that sim state will not differ from state from services due to delays between queries
                yield return To.Exec(PauseExecution, true);
               
                //Get testee state from simulator and deserialize it
                IEnumerable<MrsePxy.VisualEntity> testeeEntitiesPxies = null;
                yield return To.Exec(GetAndDeserializeEntityPxies,
                        (IEnumerable<MrsePxy.VisualEntity> ens) => testeeEntitiesPxies = ens,
                        (Func<XmlElement, bool>) (xe => testInfo.TestAllEntities || IsTesteeXmlNode(xe)));
                LogInfo(SimulationTesterLogCategory.TestTesteeEntitiesDeserialized, fixtureInfo.Name, testInfo.Name, i, testeeEntitiesPxies.Count());

                //Check test's result
                var testSucceed = false;
                if (testInfo.Test != null)
                {
                    yield return To.Exec(testInfo.Test, b => testSucceed = b, testeeEntitiesPxies, dt);
                    LogInfo(SimulationTesterLogCategory.TestResultsAssessed, fixtureInfo.Name, testInfo.Name, i, testSucceed);
                }

                OnTestTryEnded(testInfo, testSucceed);
                if (testSucceed) ++successful;
                LogInfo(SimulationTesterLogCategory.TestRunningResult, fixtureInfo.Name, testInfo.Name, (float)successful / i);

	            //Drop services that need to be restarted
				yield return To.Exec(DropServices, (Func<Uri, bool>)(uri => uri.LocalPath.Contains(RESET_SYMBOL)));
                LogInfo(SimulationTesterLogCategory.TestServicesDropped, fixtureInfo.Name, testInfo.Name, i);
            }

            LogInfo(SimulationTesterLogCategory.TestFinished, fixtureInfo.Name, testInfo.Name, (float)successful / i);
            @return(_state.FastCheck ? 1 : (float)successful / i);
        }

		IEnumerator<ITask> SetUpSimulator(SimulationTestFixtureInfo testFixtureInfo)
        {
			yield return To.Exec(_simEngine.UpdatePhysicsTimeStep(testFixtureInfo.PhysicsTimeStep < 0 ? -1 : testFixtureInfo.PhysicsTimeStep));
			//yield return To.Exec(_simEngine.UpdateSimulatorConfiguration(new EngPxy.SimulatorConfiguration { Headless = true }));

			//_initialSimState.RenderMode = testFixtureInfo.ToRender ? MrsPxy.RenderMode.Full : MrsPxy.RenderMode.None;
			//yield return To.Exec(_simEngine.Replace(_initialSimState));
        }

        IEnumerator<ITask> StartManifest(string manifest)
        {
            yield return To.Exec(_manifestLoader.Insert(new InsertRequest
                {
                    Manifest = new UriBuilder(ServiceInfo.HttpServiceAlias.Scheme, ServiceInfo.HttpServiceAlias.Host, ServiceInfo.HttpServiceAlias.Port,
                                              String.Format(@"{0}/{1}/{2}.{3}", ServicePaths.MountPoint, TESTS_PATH, manifest, MANIFEST_EXTENSION)).ToString()
                }));
        }

		IEnumerator<ITask> DropServices(Func<Uri, bool> predicate)
		{
			IEnumerable<Uri> runningServices = null;
			yield return To.Exec(GetRunningServices, (IEnumerable<Uri> ss) => runningServices = ss);
			foreach (var uri in runningServices.Where(predicate).Distinct(new ServiceUriComparer()))
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
                dropped => LogInfo(String.Format("Service {0} dropped!", uri)),
                failed => LogError(String.Format("Service {0} can not be dropped", uri)));
        }

        IEnumerator<ITask> PauseExecution(bool pause)
        {
            yield return To.Exec(PauseSimulator, pause);
            yield return To.Exec(Timer.Pause(pause));
        }

        IEnumerator<ITask> PauseSimulator(bool pause)
        {
            _initialSimState.Pause = pause;
            yield return To.Exec(_simEngine.Replace(_initialSimState));
        }

        IEnumerator<ITask> RestoreEnvironment(string environmentXmlFile, Func<Mrse.VisualEntity, bool> resetFilter, Action<Mrse.VisualEntity> prepareEntityForReset)
        {
            yield return To.Exec(PauseSimulator, true);

            MrsPxy.SimulationState simState = null;
            yield return _simEngine.Get().Choice(st => simState = st, LogError);

            IEnumerable<Mrse.VisualEntity> entities = null;
            yield return To.Exec(_entityDeserializer.DeserializeTopLevelEntities, (IEnumerable<Mrse.VisualEntity> es) => entities = es, simState.SerializedEntities);

            foreach (var entity in entities.Where(e => resetFilter(e) || e.State.Name == "timer"))
                yield return To.Exec(DeleteEntity(entity));

            yield return To.Exec(PauseSimulator, false);
            yield return TimeoutPort(100).Receive();

            var get = new DsspDefaultGet();
            ServiceForwarder<MountServiceOperations>(String.Format(@"{0}/{1}/{2}.{3}", ServicePaths.MountPoint, TESTS_PATH, environmentXmlFile, ENVIRONMENT_EXTENSION)).Post(get);
            yield return get.ResponsePort.Choice(LogError, success => simState = (MrsPxy.SimulationState)success);

            yield return To.Exec(_entityDeserializer.DeserializeTopLevelEntities, (IEnumerable<Mrse.VisualEntity> es) => entities = es, simState.SerializedEntities);

            foreach (var entity in entities.Where(resetFilter))
			{
				if (prepareEntityForReset != null)
					prepareEntityForReset(entity);
				yield return To.Exec(InsertEntity(entity));
			}

            yield return To.Exec(InsertEntity(new TimerEntity("timer")));

            yield return To.Exec(UpdateCameraView(new Mrse.CameraView
            {
                EyePosition = (Vector3) DssTypeHelper.TransformFromProxy(simState.CameraPosition),
                LookAtPoint = (Vector3) DssTypeHelper.TransformFromProxy(simState.CameraLookAt)
            }));
        }

        IEnumerator<ITask> GetAndDeserializeEntityPxies(Action<IEnumerable<MrsePxy.VisualEntity>> @return, Func<XmlElement, bool> filter)
        {
            MrsPxy.SimulationState simState = null;
            yield return _simEngine.Get().Choice(st => simState = st, LogError);
            yield return To.Exec(_entityDeserializer.DeserializeTopLevelEntityPxies, @return, simState.SerializedEntities, filter);
        }

        PortSet<DefaultInsertResponseType, Fault> InsertEntity(Mrse.VisualEntity entity)
        {
            var insRequest = new Mrse.InsertSimulationEntity(entity);
            Mrse.SimulationEngine.GlobalInstancePort.Post(insRequest);
            return insRequest.ResponsePort;
        }

        PortSet<DefaultDeleteResponseType, Fault> DeleteEntity(Mrse.VisualEntity entity)
        {
            var delRequest = new Mrse.DeleteSimulationEntity(entity);
            Mrse.SimulationEngine.GlobalInstancePort.Post(delRequest);
            return delRequest.ResponsePort;
        }

        PortSet<DefaultUpdateResponseType, Fault> UpdateCameraView(Mrse.CameraView cameraView)
        {
            var updCameraViewRq = new Mrse.UpdateCameraView(cameraView);
            Mrse.SimulationEngine.GlobalInstancePort.Post(updCameraViewRq);
            return updCameraViewRq.ResponsePort;
        }

        bool HasEarlyResults(int i, int successful)
		{
            if (!_state.FastCheck)
			    return i == TRIES_NUMBER / 10 && ((float)successful / i > SUCCESS_THRESHOLD || (float)successful / i < 1 - SUCCESS_THRESHOLD);
            else
                return successful > 0;
		}

        static bool IsTesteeXmlNode(XmlElement xe)
        {
            return xe.SelectSingleNode(@"/*[local-name()='State']/*[local-name()='Name']/text()").InnerText.Contains(RESET_SYMBOL);
        }
	}
}