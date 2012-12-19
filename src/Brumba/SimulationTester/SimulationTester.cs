using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using SimPxy = Microsoft.Robotics.Simulation.Proxy;
using Microsoft.Robotics.Simulation.Engine;
using SafwPxy = Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using StPxy = Brumba.Simulation.SimulatedTimer.Proxy;
using System.Linq;
using System.Xml;
using Xna = Microsoft.Xna.Framework;
using Microsoft.Dss.Services.Serializer;
using Microsoft.Dss.Core;
using Microsoft.Dss.Services.MountService;
using MountPxy = Microsoft.Dss.Services.MountService;
using Brumba.Simulation.SimulatedTimer;

namespace Brumba.Simulation.SimulationTester
{
	[Contract(Contract.Identifier)]
	[DisplayName("SimulationTester")]
	[Description("SimulationTester service (no description provided)")]
	class SimulationTesterService : DsspServiceBase, IServiceStarter
	{
		public const int TRIES_NUMBER = 10;
		public const SimPxy.RenderMode RENDER_MODE = SimPxy.RenderMode.None;
		//public const SimPxy.RenderMode RENDER_MODE = SimPxy.RenderMode.Full;

		[ServiceState]
		SimulationTesterState _state = new SimulationTesterState();
		
		[ServicePort("/SimulationTester", AllowMultipleInstances = true)]
		SimulationTesterOperations _mainPort = new SimulationTesterOperations();

        [Partner("SimEngine", Contract = Microsoft.Robotics.Simulation.Engine.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        EngPxy.SimulationEnginePort _simEngine = new EngPxy.SimulationEnginePort();

        [Partner("SimTimer", Contract = SimulatedTimer.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UsePartnerListEntry)]
        StPxy.SimulatedTimerOperations _timer = new StPxy.SimulatedTimerOperations();

        private readonly List<ISimulationTestFixture> _testFixtures = new List<ISimulationTestFixture>();
		
		public SimulationTesterService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}
		
		protected override void Start()
		{
			base.Start();

            _testFixtures.Add(new SimpleAckermanVehTests());
            _testFixtures.Add(new Simple4x4AckermanVehTests());

            SpawnIterator(ExecuteTests);
		}

        IEnumerator<ITask> ExecuteTests()
        {
            yield return To.Exec(SetUpSimulator);

            SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort = null;
            yield return To.Exec(SetUpTest1Services, (SafwPxy.SimulatedAckermanFourWheelsOperations vp) => vehiclePort = vp);
            yield return To.Exec(TimeoutPort(50));

            Console.WriteLine();
            foreach (var fixture in _testFixtures)
            {
                Console.WriteLine("Fixture {0}", fixture.GetType().Name);
                yield return To.Exec(RestoreTestEnvironment, fixture.Tests.First(), new Func<IEnumerable<EngPxy.VisualEntity>, IEnumerable<EngPxy.VisualEntity>>(es => es), new Func<IEnumerable<VisualEntity>, IEnumerable<VisualEntity>>(es => es));

                foreach (var test in fixture.Tests)
                {
                    Console.Write("\t{0,20} ", test.GetType().Name);
                    float result = 0;
                    yield return To.Exec(ExecuteTest, (float r) => result = r, test, vehiclePort);
                    WriteColored(result * 100 > 80 ? ConsoleColor.Green : ConsoleColor.Red, " {0}%", result * 100);
                    Console.WriteLine();
                }
            }
        }

        IEnumerator<ITask> SetUpTest1Services(Action<SafwPxy.SimulatedAckermanFourWheelsOperations> @return)
        {
            SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort = null;
            yield return Arbiter.Choice(AckermanFourWheelsCreator.StartService(this, "testee"), vp => vehiclePort = vp, LogError);
            if (vehiclePort == null)
                yield break;
            @return(vehiclePort);
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

        IEnumerator<ITask> ExecuteTest(Action<float> @return, ISimulationTest test, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
        	var successful = 0;
			for (int i = 0; i < TRIES_NUMBER; ++i)
            {
                yield return To.Exec(RestoreTestEnvironment, test, (Func<IEnumerable<EngPxy.VisualEntity>, IEnumerable<EngPxy.VisualEntity>>)test.FindEntitiesToRestore, (Func<IEnumerable<VisualEntity>, IEnumerable<VisualEntity>>)test.PrepareEntitiesToRestore);

                yield return To.Exec(test.Start, vehiclePort);

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
                WriteColored(testSucceed ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed, testSucceed ? "." : "x");

                if (testSucceed) ++successful;
            }

            @return((float)successful / TRIES_NUMBER);
        }

	    void WriteColored(ConsoleColor color, string text, params object[] objs)
        {
            Console.ForegroundColor = color;
            Console.Write(text, objs);
            Console.ResetColor();
        }

        IEnumerator<ITask> RestoreTestEnvironment(ISimulationTest test, Func<IEnumerable<EngPxy.VisualEntity>, IEnumerable<EngPxy.VisualEntity>> deleteFilter, Func<IEnumerable<VisualEntity>, IEnumerable<VisualEntity>> insertFilter)
        {
            SimPxy.SimulationState simState = null;
            yield return Arbiter.Choice(_simEngine.Get(), st => simState = st, LogError);

            var renderMode = simState.RenderMode;
            simState.Pause = true;
            yield return To.Exec(_simEngine.Replace(simState));

            IEnumerable<EngPxy.VisualEntity> entityPxies = null;
			yield return To.Exec(DeserializaTopLevelEntityProxies, (IEnumerable<EngPxy.VisualEntity> ePxies) => entityPxies = ePxies, simState);
            foreach (var entity in deleteFilter(entityPxies).Union(entityPxies.Where(pxy => pxy.State.Name == "timer")))
                yield return To.Exec(_simEngine.DeleteSimulationEntity((EngPxy.VisualEntity)DssTypeHelper.TransformToProxy(entity)));

            simState.Pause = false;
            simState.RenderMode = renderMode;
            yield return To.Exec(_simEngine.Replace(simState));

            var get = new DsspDefaultGet();
            ServiceForwarder<MountServiceOperations>(String.Format(@"{0}/brumba/src/brumba/simulationtester/{1}", ServicePaths.MountPoint, test.Fixture.EnvironmentXmlFile)).Post(get);
            yield return Arbiter.Choice(get.ResponsePort, LogError, success => simState = (Microsoft.Robotics.Simulation.Proxy.SimulationState)success);

            IEnumerable<VisualEntity> entities = null;
            yield return To.Exec(DeserializeTopLevelEntities, (IEnumerable<VisualEntity> ens) => entities = ens, simState);
			foreach (var insRequest in insertFilter(entities).Select(entity => new InsertSimulationEntity(entity)))
			{
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

        #region IServiceCreator
        DsspResponsePort<CreateResponse> IServiceStarter.CreateService(ServiceInfoType serviceInfo)
        {
            return CreateService(serviceInfo);
        }

        SafwPxy.SimulatedAckermanFourWheelsOperations IServiceStarter.ServiceForwarder(Uri uri)
        {
            return ServiceForwarder<SafwPxy.SimulatedAckermanFourWheelsOperations>(uri);
        }

        void IServiceStarter.Activate(Choice choice)
        {
            Activate(choice);
        }
        #endregion
	}
}


