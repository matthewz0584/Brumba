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

namespace Brumba.Simulation.SimulationTester
{
	[Contract(Contract.Identifier)]
	[DisplayName("SimulationTester")]
	[Description("SimulationTester service (no description provided)")]
	class SimulationTesterService : DsspServiceBase, IServiceStarter
	{
        private const string TERRAIN_FILE = @"terrain_file.bmp";
        private const string TERRAIN_PATH = @"store\media\";

		[ServiceState]
		SimulationTesterState _state = new SimulationTesterState();
		
		[ServicePort("/SimulationTester", AllowMultipleInstances = true)]
		SimulationTesterOperations _mainPort = new SimulationTesterOperations();

        [Partner("SimEngine", Contract = Microsoft.Robotics.Simulation.Engine.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        EngPxy.SimulationEnginePort _simEngine = new EngPxy.SimulationEnginePort();
		
		public SimulationTesterService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}
		
		protected override void Start()
		{
			base.Start();

            SpawnIterator(Test1);
		}

        private IEnumerator<ITask> Test1()
        {
            yield return To.Exec(SetUpSimulator);

            SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort = null;
            yield return To.Exec(SetUpTest1Services, (SafwPxy.SimulatedAckermanFourWheelsOperations vp) => vehiclePort = vp);

            yield return To.Exec(SetUpTest1Environment, true);

            int successful = 0;
            for (int i = 0; i < 100; ++i)
            {
                yield return To.Exec(SetUpTest1Environment, false);

                double estimatedTime = 0;
                yield return To.Exec(StartTest1, (double et) => estimatedTime = et, vehiclePort);
//Console.WriteLine("Test Started");

                bool test1Succeed = false;
                double startTime = 0.0, elapsedTime = 0.0;

                yield return Arbiter.Choice(vehiclePort.Get(), s => startTime = s.ElapsedTime, LogError);
//Console.WriteLine("Time queried 1");
                elapsedTime = startTime;
                while (!test1Succeed && elapsedTime - startTime <= estimatedTime)
                {
                    SimPxy.SimulationState simState = null;
//Console.WriteLine("Simstate query");
                    yield return Arbiter.Choice(_simEngine.Get(), st => simState = st, LogError);
//Console.WriteLine("Simstate queried");

                    yield return To.Exec(AssessTestProgress, (bool b) => test1Succeed = b, simState);

                    yield return Arbiter.Choice(vehiclePort.Get(), s => elapsedTime = s.ElapsedTime, LogError);
//Console.WriteLine("Time queried 2");

                    if (!test1Succeed)
                        yield return To.Exec(TimeoutPort(50));
                }
//yield return To.Exec(TimeoutPort(400));
Console.WriteLine("{0} was {1}", i, test1Succeed);
                
                if (test1Succeed) ++successful;
            }

Console.WriteLine("test1 result - {0}", successful / 100);
            
            //_mainPort.Post(new DsspDefaultDrop());
        }

        IEnumerator<ITask> SetUpTest1Services(Action<SafwPxy.SimulatedAckermanFourWheelsOperations> @return)
        {
            SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort = null;
            yield return Arbiter.Choice(AckermanFourWheelsCreator.StartService(this, "testee"), vp => vehiclePort = vp, LogError);
            if (vehiclePort == null)
                yield break;
            @return(vehiclePort);
        }

        IEnumerator<ITask> StartTest1(Action<double> @return, SafwPxy.SimulatedAckermanFourWheelsOperations vehiclePort)
        {
            float motorPower = 1f;
            yield return To.Exec(vehiclePort.SetMotorPower(new SafwPxy.MotorPowerRequest { Value = motorPower }));
            @return(2 * 50 / (AckermanFourWheelsEntity.Builder.Default.MaxVelocity * motorPower));//50 meters
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

        private IEnumerator<ITask> AssessTestProgress(Action<bool> @return, SimPxy.SimulationState simState)
        {
            var vehNode = simState.SerializedEntities.XmlNodes.Cast<XmlElement>().Where(xn => xn.Name == "AckermanFourWheelsEntity").Single();

            EngPxy.VisualEntity vehEntity = null;
            yield return To.Exec(DeserializeEntityFromXml, (EngPxy.VisualEntity e) => vehEntity = e, vehNode);

            var pos = vehEntity.State.Pose.Position;
            @return(new Xna.Vector3(pos.X, pos.Y, pos.Z).Length() > 50);
        }

        private IEnumerator<ITask> SetUpTest1Environment(bool everything)
        {
            SimPxy.SimulationState simState = null;
            yield return Arbiter.Choice(_simEngine.Get(), st => simState = st, LogError);

            var renderMode = simState.RenderMode;
            simState.Pause = true;
            yield return To.Exec(_simEngine.Replace(simState));

//Console.WriteLine("Pause On");

            IEnumerable<EngPxy.VisualEntity> entities = null;
            yield return To.Exec(DeserializaTopLevelEntities, (IEnumerable<EngPxy.VisualEntity> ens) => entities = ens, simState, everything);
            foreach (var entity in entities)
                yield return To.Exec(_simEngine.DeleteSimulationEntity(entity));

            yield return To.Exec(TimeoutPort(100));
            
            var mountService = ServiceForwarder<MountServiceOperations>(ServicePaths.MountPoint + "/brumba/simulationtester/test1.xml");
            var get = new DsspDefaultGet();
            mountService.Post(get);
            yield return Arbiter.Choice(get.ResponsePort, LogError, success => simState = (Microsoft.Robotics.Simulation.Proxy.SimulationState)success);

            //IEnumerable<EngPxy.VisualEntity> entities = null;
            yield return To.Exec(DeserializaTopLevelEntities, (IEnumerable<EngPxy.VisualEntity> ens) => entities = ens, simState, everything);
            foreach (var entity in entities)
                yield return To.Exec(_simEngine.InsertSimulationEntity(entity));

            simState.Pause = false;
            simState.RenderMode = renderMode;
            yield return To.Exec(_simEngine.Replace(simState));
//Console.WriteLine("Pause Off");
        }

        private IEnumerator<ITask> DeserializaTopLevelEntities(Action<IEnumerable<EngPxy.VisualEntity>> @return, SimPxy.SimulationState simState, bool everything)
        {
            var entities = new List<EngPxy.VisualEntity>();
            foreach (var entityNode in simState.SerializedEntities.XmlNodes.Cast<XmlElement>())
            {
                EngPxy.VisualEntity entity = null;
                yield return To.Exec(DeserializeEntityFromXml, (EngPxy.VisualEntity e) => entity = e, entityNode);
                if (!everything && !(entity is SafwPxy.AckermanFourWheelsEntity))
                    continue;
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


