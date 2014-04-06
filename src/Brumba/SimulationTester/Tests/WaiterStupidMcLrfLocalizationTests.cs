//using System.Collections.Generic;
//using System.Drawing;
//using Brumba.DsspUtils;
//using Microsoft.Ccr.Core;
//using Microsoft.Dss.Core.Utilities;
//using DrivePxy = Microsoft.Robotics.Services.Drive.Proxy;
//using McLocalizationPxy = Brumba.WaiterStupid.McLocalization.Proxy;
//using WaiterStupidPxy = Brumba.WaiterStupid.Proxy;

//namespace Brumba.SimulationTester.Tests
//{
//    [SimTestFixture("waiter_stupid_mc_lrf_localization_tests")]
//    public class WaiterStupidMcLrfLocalizationTests
//    {
//        public SimulationTesterService TesterService { get; private set; }
//        public DrivePxy.DriveOperations RefPlDrivePort { get; private set; }
//        public McLocalizationPxy.McLrfLocalizerOperations McLrfLocalizationPort { get; private set; }

//        [SetUp]
//        public void SetUp(SimulationTesterService testerService)
//        {
//            TesterService = testerService;
//            RefPlDrivePort = testerService.ForwardTo<DrivePxy.DriveOperations>("stupid_waiter_ref_platform/differentialdrive");
//            McLrfLocalizationPort = testerService.ForwardTo<McLocalizationPxy.McLrfLocalizerOperations>("localization@");
//        }

//        [SimTest(10)]
//        public class Tracking// : IStart
//        {
//            [Fixture]
//            public WaiterStupidMcLrfLocalizationTests Fixture { get; set; }

//            public IEnumerator<ITask> Start()
//            {
//                yield return To.Exec(Fixture.McLrfLocalizationPort.Replace(new McLocalizationPxy.McLrfLocalizerState
//                {
//                    DeltaT = 0.3f,
//                    //FirstPoseCandidate = new Pose().
//                    Map = new McLocalizationPxy.OccupancyGrid
//                    {
//                        CellSize = 0.1f,
//                        Data = new DssTwoDimArray<bool>(new FloorPlanBuilder.FloorPlanBuilder(
//                            new EnvironmentBuilderState
//                        {
//                            BoxTypes = { new BoxType { ColorOnImage = Color.Black } },
//                            FloorType = new ObjectType { ColorOnImage = Color.White },
//                            GridCellSize = 0.1f
//                        }).CreateOccupancyGrid())
//                    }
//                }));
//            }
//        }
//    }
//}