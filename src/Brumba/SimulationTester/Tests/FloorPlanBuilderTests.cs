using System;
using System.Collections.Generic;
using System.Linq;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine;
using Mrpm = Microsoft.Robotics.PhysicalModel;
using MrpmProxy = Microsoft.Robotics.PhysicalModel.Proxy;
using SingleShapeEntity = Microsoft.Robotics.Simulation.Engine.Proxy.SingleShapeEntity;
using VisualEntity = Microsoft.Robotics.Simulation.Engine.Proxy.VisualEntity;

namespace Brumba.SimulationTester.Tests
{
	[SimTestFixture("floor_plan_builder")]
	public class FloorPlanBuilderTests
	{
		// [SimTest(1f, IsProbabilistic = false, TestAllEntities = true)]
		public class SimpleObjectsTest
		{
			[Test]
			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simStateEntities, double elapsedTime)
			{
				var boxes = simStateEntities.Where(e => e is SingleShapeEntity && (e as SingleShapeEntity).BoxShape != null).Cast<SingleShapeEntity>();
				var box1 = boxes.Any(b =>
					EqualsWithin(new Mrpm.Vector4(0.0f, 0.0f, 1, 0.5f), b.BoxShape.BoxState.DiffuseColor, 0.01f) &&
					EqualsWithin(new Mrpm.Vector3(0.698f, 1, 1.298f), b.BoxShape.BoxState.Dimensions, 0.01f) &&
					EqualsWithin(new Mrpm.Vector3(7.8f, 0.5f, 5.20000029f), b.State.Pose.Position, 0.01f));
				var box2 = boxes.Any(b =>
					EqualsWithin(new Mrpm.Vector4(1f, 0, 0, 0.5f), b.BoxShape.BoxState.DiffuseColor, 0.01f) &&
					EqualsWithin(new Mrpm.Vector3(2.198f, 0.5f, 0.698f), b.BoxShape.BoxState.Dimensions, 0.01f) &&
					EqualsWithin(new Mrpm.Vector3(-1.35f, 0.25f, -3.9f), b.State.Pose.Position, 0.01f));
				@return(box1 && box2);
				yield break;
			}
		}

		public static bool EqualsWithin(Mrpm.Vector4 vec, MrpmProxy.Vector4 proxyVec, float tolerance)
		{
			return TypeConversion.ToXNA(vec).EqualsWithin(TypeConversion.ToXNA((Mrpm.Vector4)DssTypeHelper.TransformFromProxy(proxyVec)), tolerance);
		}

		public static bool EqualsWithin(Mrpm.Vector3 vec, MrpmProxy.Vector3 proxyVec, float tolerance)
		{
			return TypeConversion.ToXNA(vec).EqualsWithin(TypeConversion.ToXNA((Mrpm.Vector3)DssTypeHelper.TransformFromProxy(proxyVec)), tolerance);
		}
	}
}