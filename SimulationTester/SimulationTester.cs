using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;
using EngineProxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using SimulationProxy = Microsoft.Robotics.Simulation.Proxy;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Brumba.Simulation.SimulatedAckermanFourWheels;
using SafwProxy = Brumba.Simulation.SimulatedAckermanFourWheels.Proxy;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Diagnostics;
using Xna = Microsoft.Xna.Framework;
using System.Globalization;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.Simulation;
using Microsoft.Dss.Services.Serializer;

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
        EngineProxy.SimulationEnginePort _simEngine = new EngineProxy.SimulationEnginePort();
		
		public SimulationTesterService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}
		
		protected override void Start()
		{
			base.Start();

            SpawnIterator(Test1);
		}

        IEnumerator<ITask> SetUpSimulator()
        {
            yield return ArbiterMy.Exec(_simEngine.UpdatePhysicsTimeStep(0.01f));
            //yield return Exec(_simEngine.UpdateSimulatorConfiguration(new EngineProxy.SimulatorConfiguration { Headless = true }));
            
            var simState = new RetVal<SimulationProxy.SimulationState>();
            yield return ArbiterMy.Receive(_simEngine.Get(), simState);

            //simState.V.RenderMode = SimulationProxy.RenderMode.None;
            yield return ArbiterMy.Exec(_simEngine.Replace(simState.V));
        }

        private SafwProxy.SimulatedAckermanFourWheelsOperations _vehiclePort;
        private IEnumerator<ITask> Test1()
        {
            yield return new IterativeTask(SetUpSimulator);

            GenerateEnvironment();

            yield return GenerateTestee();

            if (_vehiclePort == null)
                yield break;

            float motorPower = 0.5f;
            _vehiclePort.SetMotorPower(new SafwProxy.MotorPowerRequest { Value = motorPower });


            bool distanceCovered = false;
            var estimatedTime = 2 * 50 / (AckermanFourWheelsEntity.Builder.Default.MaxVelocity * motorPower);//50 meters
            var startTime = 0.0;
            yield return Arbiter.Receive<SafwProxy.SimulatedAckermanFourWheelsState>(false, _vehiclePort.Get(), s => { startTime = s.ElapsedTime; } );
            while (!distanceCovered)
            {
                var elapsedTime = 0.0;
                yield return Arbiter.Receive<SafwProxy.SimulatedAckermanFourWheelsState>(false, _vehiclePort.Get(), s => { elapsedTime = s.ElapsedTime; });
                if (elapsedTime - startTime > estimatedTime)
                    break;

                SimulationProxy.SimulationState simState = null;
                yield return Arbiter.Choice(_simEngine.Get(),
                    st => simState = st,
                    f => LogError(""));

                var rv = new RetVal<bool>();
                yield return ArbiterMy.FromIteratorHandler(AssessTestProgress, simState, rv);
                distanceCovered = rv.V;
                
                if (!distanceCovered)                
                    yield return Arbiter.Receive(false, TimeoutPort(100), dt => { });
            }



            //_mainPort.Post(new DsspDefaultDrop());
        }

        private IEnumerator<ITask> AssessTestProgress(SimulationProxy.SimulationState simState, RetVal<bool> retVal)
        {
            var vehNode = simState.SerializedEntities.XmlNodes.Cast<XmlElement>().Where(xn => xn.Name == "AckermanFourWheelsEntity").Single();
            var desRequest = new Deserialize(new XmlNodeReader(vehNode));
            SerializerPort.Post(desRequest);

            var desVeh = new RetVal<DeserializeResult>();
            yield return ArbiterMy.ReceiveFaulty(desRequest.ResultPort, desVeh, LogError);

            retVal.V = TypeConversion.ToXNA(((VisualEntity)DssTypeHelper.TransformFromProxy(desVeh.V.Instance)).State.Pose.Position).Length() > 50;
        }

        private void GenerateEnvironment()
        {
            CrossCountryGenerator.Generate(257, 0.1f).Save(TERRAIN_PATH + TERRAIN_FILE);

            var terrain = new TerrainEntity(TERRAIN_FILE, "terrain_tex.jpg", new MaterialProperties("ground", 0, 0.5f, 1.0f))
            {
                State = { Name = "Terrain", Assets = { Effect = "Terrain.fx" } },
            };
            SimulationEngine.GlobalInstancePort.Insert(terrain);

            var view = new CameraView { EyePosition = new Vector3(-1.65f, 1.63f, -0.29f), LookAtPoint = new Vector3(0, 0, 0) };
            SimulationEngine.GlobalInstancePort.Update(view);

            SkyDomeEntity sky = new SkyDomeEntity("skydome.dds", "sky_diff.dds");
            SimulationEngine.GlobalInstancePort.Insert(sky);

            var sun = new LightSourceEntity
            {
                State = { Name = "Sun" },
                Type = LightSourceEntityType.Directional,
                Color = new Vector4(0.8f, 0.8f, 0.8f, 1),
                Direction = new Vector3(0.5f, -.75f, 0.5f)
            };
            SimulationEngine.GlobalInstancePort.Insert(sun);
        }

        private ITask GenerateTestee()
        {
            return Arbiter.Choice(AckermanFourWheelsCreator.Insert(this, "testee", new Vector3(0, 0.1f, 0), AckermanFourWheelsEntity.Builder.Default),
                           ops4 => _vehiclePort = ops4,
                           f => LogInfo("bebebe"));
        }

        #region IServiceCreator
        DsspResponsePort<CreateResponse> IServiceStarter.CreateService(ServiceInfoType serviceInfo)
        {
            return CreateService(serviceInfo);
        }

        SafwProxy.SimulatedAckermanFourWheelsOperations IServiceStarter.ServiceForwarder(Uri uri)
        {
            return ServiceForwarder<SafwProxy.SimulatedAckermanFourWheelsOperations>(uri);
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


