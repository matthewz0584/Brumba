﻿using System;
using System.Collections.Generic;
using Brumba.MapProvider;
using Brumba.SimulationTestRunner;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Simulation.Engine.Proxy;
using Microsoft.Xna.Framework;
using MapPxy = Brumba.MapProvider.Proxy;

namespace Brumba.AcceptanceTests
{
	[SimTestFixture("map_provider")]
	public class MapProviderTests
	{
	    public MapPxy.MapProviderOperations MapProviderPort { get; set; }

	    [SetUp]
        public void SetUp(SimulationTestRunnerService hostService)
		{
		    MapProviderPort = hostService.ForwardTo<MapPxy.MapProviderOperations>("map_provider");
		}

	    [SimTest(1, IsProbabilistic = false)]
		public class GetTest : ITest
		{
			[Fixture]
            public MapProviderTests Fixture { get; set; }

			public IEnumerator<ITask> Test(Action<bool> @return, IEnumerable<VisualEntity> simEntities, double elapsedTime)
			{
                yield return Fixture.MapProviderPort.Get().Choice(
                    mps => @return(CheckOccupancyGrid((OccupancyGrid)DssTypeHelper.TransformFromProxy(mps.Map))),
                    f => @return(false));
            }

		    static bool CheckOccupancyGrid(OccupancyGrid occGrid)
		    {
                occGrid.Freeze();
		        if (!(occGrid.SizeInCells == new Point(220, 180) && occGrid.CellSize == 0.1f))
                    return false;

		        for (var y = 0; y < 180; ++y)
		            for (var x = 0; x < 220; ++x)
						if ((y >= (180 - 1 - 43) && y <= (180 - 1 - 31) && x >= 28 && x <= 34) ||
							(y >= (180 - 1 - 131) && y <= (180 - 1 - 125) && x >= 112 && x <= 133))
		                {
		                    if (!occGrid[x, y])
		                        return false;
		                }
		                else if (occGrid[x, y])
		                    return false;
		        return true;
		    }
		}
	}
}