using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;
using EngPxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using SimPxy = Microsoft.Robotics.Simulation.Proxy;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Brumba.Simulation.SimulatedAckermanFourWheels;
using SafwPxy = Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using StPxy = Brumba.Simulation.SimulatedTimer.Proxy;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Diagnostics;
using Xna = Microsoft.Xna.Framework;
using System.Globalization;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.Simulation;
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
	partial class SimulationTesterService : DsspServiceBase, IServiceStarter
	{
        private const string TERRAIN_FILE = @"terrain_file.bmp";
        private const string TERRAIN_PATH = @"store\media\";

		[ServiceState]
		SimulationTesterState _state = new SimulationTesterState();
		
		[ServicePort("/SimulationTester", AllowMultipleInstances = true)]
		SimulationTesterOperations _mainPort = new SimulationTesterOperations();

        [Partner("SimEngine", Contract = Microsoft.Robotics.Simulation.Engine.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        EngPxy.SimulationEnginePort _simEngine = new EngPxy.SimulationEnginePort();

        [Partner("SimTimer", Contract = Brumba.Simulation.SimulatedTimer.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UsePartnerListEntry)]
        StPxy.SimulatedTimerOperations _timer = new StPxy.SimulatedTimerOperations();
		
		public SimulationTesterService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}
		
		protected override void Start()
		{
			base.Start();

            SpawnIterator(StartTests);
		}

        private IEnumerator<ITask> StartTests()
        {
            yield return To.Exec(SetUpSimulator);

            SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort = null;
            yield return To.Exec(SetUpTest1Services, (SafwPxy.SimulatedAckermanFourWheelsOperations vp) => vehiclePort = vp);

            yield return To.Exec(RestoreTest1Environment, (List<string>)null);

            yield return To.Exec(ExecuteTest, new Test1(), vehiclePort);
            yield return To.Exec(ExecuteTest, new Test2(), vehiclePort);
        }

        IEnumerator<ITask> ExecuteTest(Test test, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            Console.Write("Test ");

            int successful = 0;
            for (int i = 0; i < 10; ++i)
            {
                yield return To.Exec(RestoreTest1Environment, test.ObjectsToRestore());

                yield return To.Exec(test.Start, vehiclePort);

                bool testSucceed = false;
                
                double elapsedTime = 0.0;                
                while (!testSucceed && elapsedTime <= test.EstimatedTime * 2)
                {
                    SimPxy.SimulationState simState = null;
                    yield return Arbiter.Choice(_simEngine.Get(), st => simState = st, LogError);

                    IEnumerable<EngPxy.VisualEntity> simStateEntities = null;
                    yield return To.Exec(DeserializaTopLevelEntities, (IEnumerable<EngPxy.VisualEntity> ens) => simStateEntities = ens, simState);
                    yield return To.Exec(test.AssessProgress, (bool b) => testSucceed = b, simStateEntities, elapsedTime);

                    yield return Arbiter.Choice(_timer.Get(), s => elapsedTime = s.ElapsedTime, LogError);

                    if (!testSucceed)
                        yield return To.Exec(TimeoutPort(50));
                }
                //yield return To.Exec(TimeoutPort(400));
                PrintOutSingleTestResult(testSucceed);

                if (testSucceed) ++successful;
            }

            Console.WriteLine(" {0}%", (float)successful / 10 * 100);
        }

        private void PrintOutSingleTestResult(bool testSucceed)
        {
            var consoleColor = Console.ForegroundColor;
            if (testSucceed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(".");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("x");
            }
            Console.ForegroundColor = consoleColor;
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

            simState.RenderMode = SimPxy.RenderMode.None;
            yield return To.Exec(_simEngine.Replace(simState));
        }

        private IEnumerator<ITask> RestoreTest1Environment(List<string> objectsToRestore)
        {
            SimPxy.SimulationState simState = null;
            yield return Arbiter.Choice(_simEngine.Get(), st => simState = st, LogError);

            var renderMode = simState.RenderMode;
            simState.Pause = true;
            yield return To.Exec(_simEngine.Replace(simState));

//Console.WriteLine("Pause On");

            IEnumerable<EngPxy.VisualEntity> entities = null;
            yield return To.Exec(DeserializaTopLevelEntities, (IEnumerable<EngPxy.VisualEntity> ens) => entities = ens, simState);
            if (objectsToRestore != null) objectsToRestore.Add("timer");
            foreach (var entity in entities.Where(e => objectsToRestore == null || objectsToRestore.Contains(e.State.Name)))
                yield return To.Exec(_simEngine.DeleteSimulationEntity(entity));

            var mountService = ServiceForwarder<MountServiceOperations>(ServicePaths.MountPoint + "/brumba/simulationtester/test1.xml");
            var get = new DsspDefaultGet();
            mountService.Post(get);
            yield return Arbiter.Choice(get.ResponsePort, LogError, success => simState = (Microsoft.Robotics.Simulation.Proxy.SimulationState)success);

            //IEnumerable<EngPxy.VisualEntity> entities = null;
            yield return To.Exec(DeserializaTopLevelEntities, (IEnumerable<EngPxy.VisualEntity> ens) => entities = ens, simState);
            foreach (var entity in entities.Where(e => objectsToRestore == null || objectsToRestore.Contains(e.State.Name)))
                yield return To.Exec(_simEngine.InsertSimulationEntity(entity));
            
            SimulationEngine.GlobalInstancePort.Insert(new TimerEntity("timer"));

            simState.Pause = false;
            simState.RenderMode = renderMode;
            yield return To.Exec(_simEngine.Replace(simState));
//Console.WriteLine("Pause Off");
        }

        private IEnumerator<ITask> DeserializaTopLevelEntities(Action<IEnumerable<EngPxy.VisualEntity>> @return, SimPxy.SimulationState simState)
        {
            var entities = new List<EngPxy.VisualEntity>();
            foreach (var entityNode in simState.SerializedEntities.XmlNodes.Cast<XmlElement>())
            {
                EngPxy.VisualEntity entity = null;
                yield return To.Exec(DeserializeEntityFromXml, (EngPxy.VisualEntity e) => entity = e, entityNode);
                if (entity.State.Name == "MainCamera")
                    continue;
                if (entity.ParentJoint != null)
                    continue;
                entities.Add(entity);
            }
            @return(entities);
        }

        private IEnumerator<ITask> DeserializeEntityFromXml(Action<EngPxy.VisualEntity> @return, XmlElement entityNode)
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

        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public void DropHandler(DsspDefaultDrop drop)
        {
            base.DefaultDropHandler(drop);
            _simEngine.DsspDefaultDrop();
        }
	}
}


