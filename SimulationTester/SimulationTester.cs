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

namespace Brumba.Simulation.SimulationTester
{
    public class RetVal<T>
    {
        public T V { get; set; }
    }

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

        ITask Exec<T>(PortSet<T, Fault> portSet)
        {
            return Arbiter.Receive<T>(false, portSet, (T val) => {});
        }

        ITask Receive<T1, T2>(PortSet<T1, T2> portSet, RetVal<T1> retVal = null)
        {
            return Arbiter.Receive<T1>(false, portSet, (T1 val) => { if (retVal != null) retVal.V = val; });
        }

        IEnumerator<ITask> SetUpSimulator()
        {
            yield return Exec(_simEngine.UpdatePhysicsTimeStep(0.01f));
            //yield return Exec(_simEngine.UpdateSimulatorConfiguration(new EngineProxy.SimulatorConfiguration { Headless = true }));
            
            var simState = new RetVal<SimulationProxy.SimulationState>();
            yield return Receive(_simEngine.Get(), simState);

            //simState.V.RenderMode = SimulationProxy.RenderMode.None;
            yield return Exec(_simEngine.Replace(simState.V));
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
            var estimatedTime = 2 * 50 / (AckermanFourWheelsEntity.Builder.Default.MaxVelocity * motorPower);
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

                var xVeh = XElement.Load(new XmlNodeReader((simState.SerializedEntities.XmlNodes.Where(xn => (xn as XmlElement).Name == "AckermanFourWheelsEntity").Single() as XmlElement)));

                var simNs = XNamespace.Get(@"http://schemas.microsoft.com/robotics/2006/04/simulation.html");
                Debug.Assert(xVeh.Element(simNs + "State").Element(simNs + "Name").Value == "testee");

                var physNs = XNamespace.Get(@"http://schemas.microsoft.com/robotics/2006/07/physicalmodel.html");
                var x = float.Parse(xVeh.Element(simNs + "State").Element(simNs + "Pose").Element(physNs + "Position").Element(physNs + "X").Value, CultureInfo.GetCultureInfo("en-GB").NumberFormat);
                var y = float.Parse(xVeh.Element(simNs + "State").Element(simNs + "Pose").Element(physNs + "Position").Element(physNs + "Y").Value, CultureInfo.GetCultureInfo("en-GB").NumberFormat);
                var z = float.Parse(xVeh.Element(simNs + "State").Element(simNs + "Pose").Element(physNs + "Position").Element(physNs + "Z").Value, CultureInfo.GetCultureInfo("en-GB").NumberFormat);

                if (new Xna.Vector3(x, y, z).Length() > 50)
                    distanceCovered = true;
                else
                    yield return Arbiter.Receive(false, TimeoutPort(100), dt => { });
            }

            _mainPort.Post(new DsspDefaultDrop());
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


