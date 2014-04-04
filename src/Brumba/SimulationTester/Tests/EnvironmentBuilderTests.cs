using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using Mrpm = Microsoft.Robotics.PhysicalModel;
using MrpmProxy = Microsoft.Robotics.PhysicalModel.Proxy;
using SingleShapeEntity = Microsoft.Robotics.Simulation.Engine.Proxy.SingleShapeEntity;
using VisualEntity = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;
using EnvBuilderPxy = Brumba.Simulation.EnvironmentBuilder.Proxy;

namespace Brumba.SimulationTester.Tests
{
	[SimTestFixture("environment_builder")]
	public class EnvironmentBuilderTests
	{
		EnvBuilderPxy.EnvironmentBuilderOperations EnvironmentBuilderPort { get; set; }
		SimulationTesterService HostService { get; set; }

		[SetUp]
		public void SetUp(SimulationTesterService hostService)
		{
			HostService = hostService;
			EnvironmentBuilderPort = hostService.ForwardTo<EnvBuilderPxy.EnvironmentBuilderOperations>("environment_builder");
		}

		[SimTest(1f, IsProbabilistic = false, TestAllEntities = true)]
		public class BuildBoxWorldTest : IStart, ITest
		{
			[Fixture]
			public EnvironmentBuilderTests Fixture { get; set; }

			public IEnumerator<ITask> Start()
			{
				yield return Fixture.HostService.Timeout(1000);
				yield return Fixture.EnvironmentBuilderPort.BuildBoxWorld().Receive((DefaultSubmitResponseType success) => { });
			}

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				var boxes = simStateEntities.Where(e => e is SingleShapeEntity && (e as SingleShapeEntity).BoxShape != null).Cast<SingleShapeEntity>();
				var box1 = boxes.Any(b =>
					EqualsWithin(new Mrpm.Vector4(0, 0, 1, 1), b.BoxShape.BoxState.DiffuseColor, 0.01f) &&
					EqualsWithin(new Mrpm.Vector3(1.3f, 4, 0.7f), b.BoxShape.BoxState.Dimensions, 0.01f) &&
					EqualsWithin(new Mrpm.Vector3(14.25f, 2f, 3.15f), b.State.Pose.Position, 0.01f));
				var box2 = boxes.Any(b =>
					EqualsWithin(new Mrpm.Vector4(1, 0, 0, 1), b.BoxShape.BoxState.DiffuseColor, 0.01f) &&
					EqualsWithin(new Mrpm.Vector3(0.7f, 2, 2.2f), b.BoxShape.BoxState.Dimensions, 0.01f) &&
					EqualsWithin(new Mrpm.Vector3(5.15f, 1, 12.3f), b.State.Pose.Position, 0.01f));
				@return(box1 && box2);
				yield break;
			}
		}

		public static bool EqualsWithin(Mrpm.Vector4 vec, MrpmProxy.Vector4 proxyVec, float tolerance)
		{
			return TypeConversion.ToXNA(vec).EqualsRelatively(TypeConversion.ToXNA((Mrpm.Vector4)DssTypeHelper.TransformFromProxy(proxyVec)), tolerance);
		}

		public static bool EqualsWithin(Mrpm.Vector3 vec, MrpmProxy.Vector3 proxyVec, float tolerance)
		{
			return TypeConversion.ToXNA(vec).EqualsRelatively(TypeConversion.ToXNA((Mrpm.Vector3)DssTypeHelper.TransformFromProxy(proxyVec)), tolerance);
		}
	}
}