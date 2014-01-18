//------------------------------------------------------------------------------
// MazeSimulator.cs
//
// Updated by Trevor Taylor 26-Aug-2006
// Updated again 19-Sep-2006 for the September CTP
// Updated 7-Oct-2006 for October CTP
// Recompiled under the November CTP on 8-Nov-2006
// Updated for V1.0 on 15-Dec-2006
//
// May/June 2007:
// Modified slightly for a new Simulated Differential Drive entity
// so that RotateDegrees and DriveDistance could be used with the
// V1.5 CTP.
//
// These functions were added in V1.5, but they have bugs so I still
// use my own version. Also, for use with the ExplorerSim I need to
// get the robot's pose which is not implemented in V1.5, but is in
// my version.
//
// July 2007:
// Updated for new directory structure (as recommended by Microsoft)
// and change in behaviour in V1.5 for config files.
//
// October 2007:
// Minor change to move the meshes for the robots into the entities
// which is where they belong. (Original code by Microsoft!)
//
// December 2007:
// File a niggling problem with the location of the maze image
// Release for ProMRDS
// 
// This code is freely available
//
// IMPORTANT NOTE: Please read the documentation for information
// on how this code works. You need to copy the wall texture
// files to the MRDS media store. If you don't set it up properly
// then you will have invisible walls!!!
//
// December 2008:
// Updated for RDS 2008 (V2.0)
//
// July 2009:
// Updated for RDS 2008 R2
//
// June 2010:
// Updated for RDS 2008 R3
//
// October 2011:
// Updated for RDS 4 Beta
//
// February 2012:
// Updated for RDS 4 (final release)
//
//------------------------------------------------------------------------------
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Microsoft.Robotics.Simulation.Engine;
using engineproxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;

namespace ProMRDS.Robotics.MazeSimulator
{
    [DisplayName("MazeSimulator")]
    [Description("The MazeSimulator Service")]
    [Contract(Contract.Identifier)]
    ////IMPORTANT
    public class MazeSimulatorService : DsspServiceBase
    {
        #region setup

        // TT Jul-2007 - Change in behaviour for V1.5
        // This can be overridden by specifying an initial state partner in the
        // manifest file
        public const string InitialStateUri = ServicePaths.MountPoint + @"/ProMRDS/Config/MazeSimulator.Config.xml";

        // Add an InitialStatePartner so that the config file will be read
        // NOTE: Creating a new instance of the state here will NOT
        // work if there is no config file because InitialStatePartner
        // will replace it with null!!! See the code in Start().
        [InitialStatePartner(Optional = true, ServiceUri = InitialStateUri)]
        private MazeSimulatorState _state = null;

        // TT - Increased to 16 in Oct 2006
        // Added MassMap and created a local copy of HeightMap
        // in case there were not enough items in the State.
        // Added a flag as well to create a sphere instead of a box.
        private string[] _WallTextures = new string[16];
        private Vector3[] _WallColors = new Vector3[16];
        private float[] _WallHeights = new float[16];
        private float[] _WallMasses = new float[16];
        private bool[] _UseSphere = new bool[16];

        // Port used to communicate with simulation engine service directly, no cloning
        SimulationEnginePort _simEnginePort;

        // partner attribute will cause simulation engine service to start
        [Partner("Engine",
            Contract = engineproxy.Contract.Identifier,
            CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        private engineproxy.SimulationEnginePort _engineServicePort = new engineproxy.SimulationEnginePort();

        [ServicePort("/mazesimulator", AllowMultipleInstances=false)]
        private MazeSimulatorOperations _mainPort = new MazeSimulatorOperations();

        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public MazeSimulatorService(DsspServiceCreationPort creationPort) : base(creationPort)
        {

        }

        #region properties

        static int WallCellColorThresh = -10000000;    // pixels less than this value will be counted as walls

        static bool OptimizeBlocks = true;  //can significantly reduce number of entities and thus increase frames per second
        int BlockCounter = 0;               //to count blocks after optimization

        //++ TT
        // Added for better use of bitmaps
        static bool UseBackgroundColor = true;
        static bool CenterMaze = true;

        // Simple colors
        //
        // Anyone remember CGA?
        // You don't have to list all the values in an enum,
        // but it looks pretty
        //
        public enum BasicColor : byte
        {
            Black       = 0,
            Red         = 1,
            Lime        = 2,
            Yellow      = 3,
            Blue        = 4,
            Magenta     = 5,
            Cyan        = 6,
            White       = 7,
            DarkGrey    = 8,
            Maroon      = 9,
            Green       = 10,
            Olive       = 11,
            Navy        = 12,
            Purple      = 13,
            Cobalt      = 14,
            Grey        = 15
        }

        //-- TT

        #endregion

        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            InitializeState();

            // Now save the State
            // This creates a new file the first time it is run
            // Later, it re-reads the existing file, but by then
            // the file has been populated with the defaults
            SaveState(_state);

            // Listen on the main port for requests and call the appropriate handler.
            ActivateDsspOperationHandlers();

            // Publish the service to the local Node Directory
            DirectoryInsert();

			// display HTTP service Uri
			LogInfo(LogGroups.Console, "Service uri: ");

            // Cache references to simulation/rendering and physics
            _simEnginePort = SimulationEngine.GlobalInstancePort;

            // TT Dec-2006 - Set up the initial camera view
            SetupCamera();

            // Add objects (entities) in our simulated world
            PopulateWorld();

            // Set the bounce threshold
            SpawnIterator(SetBounceThreshold);
        }

        /// <summary>
        /// Initialize the State
        /// </summary>
        void InitializeState()
        {
            int i;

            // The state might already have been created using
            // the Initial State Partner above. If so, then we
            // don't want to create a new one!
            if (_state == null)
            {
                _state = new MazeSimulatorState();
                // Do any other initialization here for the default
                // settings that you might want ...
            }

            // TT Feb-2007 - Setting the maze in the config file did not work
            // because it was initialized in the constructor for the State!
            // The maze here is one with lots of different objects including some balls
            // Note the path - It is relative to where dsshost is started from
            // Other samples are:
            // ModelSmall.gif -- A smaller model than the one above
            // office.bmp -- A black and white image of an "office" maze
            // Jul-2007:
            // Changed the location of the files
            if (_state.Maze == null || _state.Maze == "")
                _state.Maze = "ModelLarge.bmp";

            // Make sure that there is a floor texture
            // Plenty of others to try, e.g. concrete.jpg.
            if (_state.GroundTexture == null || _state.GroundTexture == "")
                _state.GroundTexture = "cellfloor.jpg";

            // TT Dec-2006 - This is a fudge to support upgrading from
            // prior versions where the RobotType did not exist. When
            // MRDS loades the config file, it does not populate any
            // of the fields that are missing. Therefore the RobotType
            // is null and this causes the code to crash later on.
            if (_state.RobotType == null)
                _state.RobotType = "Pioneer3DX";

            // Now initialize our internal copies of state info
            // This is a little bit of insurance against a bad
            // config file ...
            // Copy as many textures as available up to the max
            for (i = 0; (i < 16) && (i < _state.WallTextures.Length); i++)
            {
                _WallTextures[i] = _state.WallTextures[i];
            }
            // Fill any remaining textures with empty string
            for (; i < 16; i++)
                _WallTextures[i] = "";

            // Copy as many colors as specified
            // NOTE: The constructor for the State sets all of the
            // colors to the standard ones, so any that are not
            // specified will default to them.
            for (i = 0; (i < 16) && (i < _state.WallColors.Length); i++)
            {
                _WallColors[i] = _state.WallColors[i];
            }
            // Fill any remaining colors with the defaults
            for (; i < 16; i++)
                _WallColors[i] = MazeSimulatorState.DefaultColors[i];

            // Copy as many heights as specified
            for (i = 0; (i < 16) && (i < _state.HeightMap.Length); i++)
            {
                _WallHeights[i] = _state.HeightMap[i];
            }
            // Fill any remaining heights with the defaults
            for (; i < 16; i++)
                _WallHeights[i] = 5.0f;

            // Copy as many weights as specified
            for (i = 0; (i < 16) && (i < _state.MassMap.Length); i++)
            {
                _WallMasses[i] = _state.MassMap[i];
            }
            // Fill any remaining weights with the defaults
            for (; i < 16; i++)
                _WallMasses[i] = 0.0f;

            // Copy as many sphere flags as specified
            for (i = 0; (i < 16) && (i < _state.UseSphere.Length); i++)
            {
                _UseSphere[i] = _state.UseSphere[i];
            }
            // Fill any remaining flags with false
            for (; i < 16; i++)
                _UseSphere[i] = false;

            if (_state.SphereScale <= 0.0f)
                _state.SphereScale = 1.0f;

            if (_state.HeightScale <= 0.0f)
                _state.HeightScale = 1.0f;

            // Copy back our private versions which might have the
            // effect of extending the state
            _state.WallColors = _WallColors;
            _state.WallTextures = _WallTextures;
            _state.HeightMap = _WallHeights;
            _state.MassMap = _WallMasses;
            _state.UseSphere = _UseSphere;
        }


        /// <summary>
        /// Set Bounce Threshold - Required to use bouncing balls
        /// </summary>
        /// <returns></returns>
        IEnumerator<ITask> SetBounceThreshold()
        {
            while (PhysicsEngine.GlobalInstance == null)
                yield return Arbiter.Receive(false, TimeoutPort(100), delegate(DateTime now) { });

            PhysicsEngine.GlobalInstance.BounceThreshold = _state.BounceThreshold;
        }

        // TT Dec-2006 - Copied from Sim Tutorial 2 in V1.0
        private void SetupCamera()
        {
            // Set up initial view
            CameraView view = new CameraView();
            // TT Jul-2007 - Move back a little to see more of the field
            // Note that these values angle the camera down at 45 degrees
            // looking along the Z axis
            view.EyePosition = new Vector3(0.0f, 7.0f, -7.0f);
            view.LookAtPoint = new Vector3(0.0f, 5.0f, -5.0f);
            SimulationEngine.GlobalInstancePort.Update(view);
        }

        #endregion

        #region Basic World

        // Build the entire world view in the simulator
        private void PopulateWorld()
        {
            AddSky();
            AddGround();
            AddMaze();
        }

        void AddSky()
        {
            // Add a sky using a static texture. We will use the sky texture
            // to do per pixel lighting on each simulation visual entity
            SkyEntity sky = new SkyEntity("sky.dds", "sky_diff.dds");
            _simEnginePort.Insert(sky);
        }

        // The name says it all ...
        void AddGround()
        {
            HeightFieldShapeProperties hf = new HeightFieldShapeProperties("height field",
                64,     // number of rows 
                100,    // distance in meters, between rows
                64,     // number of columns
                100,    // distance in meters, between columns
                1,      // scale factor to multiple height values 
                -1000); // vertical extent of the height field. Should be set to large negative values

            // create array with height samples
            hf.HeightSamples = new HeightFieldSample[hf.RowCount * hf.ColumnCount];
            for (int i = 0; i < hf.RowCount * hf.ColumnCount; i++)
            {
                hf.HeightSamples[i] = new HeightFieldSample();
                hf.HeightSamples[i].Height = (short)(Math.Sin(i * 0.01));
            }

            // create a material for the entire field. We could also specify material per sample.
            hf.Material = new MaterialProperties("ground", 0.8f, 0.5f, 0.8f);

            // insert ground entity in simulation and specify a texture
            _simEnginePort.Insert(new HeightFieldEntity(hf, _state.GroundTexture));
        }

        #endregion

        #region Maze

        // TT --
        // Converted the maze to a height array instead of booleans
        // Added an offset so that the maze can be centered in world coords
        // Added color for the walls using a texture, which is quite
        // flexible because it does not require recompiling the code.
        //
        // This is tacky -- Last minute change to get the correct
        // coordinates of the image color inside AddWall(). Should
        // be handled better ... Slap on the wrist!
        int xOffset, yOffset;

        void AddMaze()
        {
            // TT - Use a more sensible approach to handling the filename
            // If the path begins with slash or backslash, assume that it is
            // a full path from the MRDS root (or mountpoint).
            // If not, then look for the file in store\media\Maze_Textures
            string mazeFilename = _state.Maze;
            if (mazeFilename.StartsWith("/") || mazeFilename.StartsWith("\\"))
                mazeFilename = LayoutPaths.RootDir.Substring(0, LayoutPaths.RootDir.Length-1) + mazeFilename;
            else
                mazeFilename = LayoutPaths.RootDir + LayoutPaths.MediaDir + "Maze_Textures\\" + mazeFilename;
            // Display the resolved filename so the user can see it in case there is a problem
            Console.WriteLine("\nLoading Maze from: " + mazeFilename);

            // TT - float, not bool
            float[,] maze = ParseImage(mazeFilename);
            int height = maze.GetLength(1);
            int width = maze.GetLength(0);
            // TT
            int[,] counters = new int[width, height];
            int tempX1, tempY1;
            int tempX2, tempY2;
            BasicColor PixelColor;

            // TT -- Allow centering of the maze
            // (Easier to find in the simulator! However, you can't
            // as easily use the pixel coordinates to figure out where
            // things are.)
            if (CenterMaze)
            {
                xOffset = (int)((-width / 2.0) + 0.5);
                yOffset = (int)((-height / 2.0) + 0.5);
            }
            else
            {
                xOffset = 0;
                yOffset = 0;
            }

            if (OptimizeBlocks)
            {
                int count;
                int thisRowCount;
                float currentHeight;

                //merge horizontal blocks
                for (int y = 0; y < height; y++)
                {
                    int x = 0;
                    while (x < width - 1)
                    {
                        //at least 2 blocks to merge
                        if (maze[x, y] > 0.0f && maze[x + 1, y] > 0.0f)
                        {
                            int startX = x;
                            // TT -- Only merge wall segments of the same height
                            count = 0;
                            currentHeight = maze[x, y];
                            while (x < width && maze[x, y] > 0.0f
                                && maze[x, y] == currentHeight)
                            {
                                maze[x, y] = 0.0f;
                                counters[x, y] = 0;
                                x++;
                                count++;
                            }
                            // TT -- Just mark the map, don't draw anything here
                            counters[startX, y] = count;
                            maze[startX, y] = currentHeight;
                        }
                        else
                        {
                            if (maze[x, y] > 0.0f)
                                counters[x, y] = 1;
                            else
                                counters[x, y] = 0;
                            x++;
                        }
                    }
                    if (x < height)
                    {
                        if (maze[x, y] > 0.0f)
                            counters[x, y] = 1;
                        else
                            counters[x, y] = 0;
                    }
                }

                //merge remaining vertical blocks
                for (int x = 0; x < width; x++)
                {
                    int y = 0;
                    while (y < height - 1)
                    {
                        //at least 2 blocks to merge
                        // Must have the same row count AND height
                        if (counters[x, y] > 0 && counters[x, y + 1] == counters[x, y]
                            && maze[x, y] == maze[x, y + 1])
                        {
                            int startY = y;
                            count = 0;
                            thisRowCount = counters[x, y];
                            // TT -- Only merge wall segments of the same height
                            currentHeight = maze[x, y];
                            while (y < height && counters[x, y] == thisRowCount
                                && maze[x, y] == currentHeight)
                            {
                                maze[x, y] = 0.0f;
                                counters[x, y] = 0;
                                y++;
                                count++;
                            }
                            // TT -- Add offset
                            tempY1 = startY + yOffset;
                            tempX1 = x + xOffset;
                            tempY2 = startY + count - 1 + yOffset;
                            tempX2 = x + thisRowCount - 1 + xOffset;
                            PixelColor = ParseColor(img.GetPixel(x, startY));
                            AddWall(tempY1, tempX1, tempY2, tempX2, currentHeight, PixelColor);
                        }
                        else
                        {
                            if (counters[x, y] > 0)
                            {
                                tempY1 = y + yOffset;
                                tempX1 = x + xOffset;
                                tempY2 = y + yOffset;
                                tempX2 = x + counters[x, y] - 1 + xOffset;
                                PixelColor = ParseColor(img.GetPixel(x, y));
                                AddWall(tempY1, tempX1, tempY2, tempX2, maze[x, y], PixelColor);
                                maze[x, y] = 0.0f;
                                counters[x, y] = 0;
                            }

                            y++;
                        }
                    }
                    // TT -- Boundary condition
                    if (y < height)
                    {
                        if (counters[x, y] > 0)
                        {
                            tempY1 = y + yOffset;
                            tempX1 = x + xOffset;
                            tempY2 = y + yOffset;
                            tempX2 = x + counters[x, y] - 1 + xOffset;
                            PixelColor = ParseColor(img.GetPixel(x, y));
                            AddWall(tempY1, tempX1, tempY2, tempX2, maze[x, y], PixelColor);
                            maze[x, y] = 0.0f;
                            counters[x, y] = 0;
                        }
                    }
                }
            }

            //draw all blocks left over
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (maze[x, y] > 0.0f)
                    {
                        // TT -- Add offset
                        tempY1 = y + yOffset;
                        tempX1 = x + xOffset;
                        PixelColor = ParseColor(img.GetPixel(x, y));
                        AddWall(tempY1, tempX1, maze[x, y], PixelColor);
                        // This is only for debugging
                        // All blocks should be zero at the end ...
                        maze[x, y] = 0.0f;
                    }
                }
            }

            if (OptimizeBlocks)
            {
                Console.WriteLine("\nOptimization reduced number of wall blocks to: " + BlockCounter +"\n");
            }

        }


        // TT -- Simple fuzzy color parsing into the 16 basic colors
        const int BlackThreshold = 16;
        const int WhiteThreshold = 208;
        const int ColorThreshold = 128;
        const double GreyTolerance = 0.10;

        // This overload is not really necessary, but I'm lazy
        BasicColor ParseColor(Color pixel)
        {
            return ParseColor(pixel.R, pixel.G, pixel.B);
        }

        BasicColor ParseColor(int r, int g, int b)
        {
            int rgbSum = r + g + b;

            // Sort out Black and White straight away.
            // The assumption here is that sufficiently
            // intense or dark colors are just white or black.
            if (rgbSum < BlackThreshold * 3)
                return BasicColor.Black;
            else if (rgbSum > WhiteThreshold * 3)
                return BasicColor.White;
            else
            {
                // Next check for grey
                // This compares the range of values to see if they
                // are basically all equal, i.e. no dominant color = grey
                float normMax, normMin, normRange;
                int valMax, valMin;
                valMax = Math.Max(r, g);
                valMax = Math.Max(valMax, b);
                valMin = Math.Min(r, g);
                valMin = Math.Min(valMin, b);
                normMax = (float)valMax;
                normMin = (float)valMin;
				normMax /= rgbSum;
				normMin /= rgbSum;
				normRange = normMax - normMin;
                if (normRange < GreyTolerance)
                {
                    if (normMax >= 160)
                        return BasicColor.Grey;
                    else
                        return BasicColor.DarkGrey;
                }

                // Now we have a more complicated task
                // but it is made easier by the definition
                // of BasicColor
                byte color = 0;

                // Check for dark versions of the colors
                if (valMax < 160)
                {
                    color += 8;
                    if (r >= ColorThreshold/2)
                        color += 1;
                    if (g >= ColorThreshold/2)
                        color += 2;
                    if (b >= ColorThreshold/2)
                        color += 4;
                }
                else
                {
                    // Now check the thresholds for normal colors
                    if (r >= ColorThreshold)
                        color += 1;
                    if (g >= ColorThreshold)
                        color += 2;
                    if (b >= ColorThreshold)
                        color += 4;
                }
                return (BasicColor)color;
            }
        }


        // Parse the image bitmap into a maze
        //
        // TT -- Modified so that you can have a background colour
        // to use for the bitmap and also to use the color as an
        // index for the height
        //
        static Bitmap img;
        float[,] ParseImage(string file)
        {
            img = (Bitmap)Image.FromFile(file);
            float[,] imgArray = new float[img.Width, img.Height];
            int WallCount = 0;

            // TT -- Allow background color to be used
            // Select the color of the top-left pixel.
            // We could be a lot smarter here. For instance, search the
            // image and find the predominant color which must be the
            // background. However, this will do for now.
            bool IsWall;
            //BasicColor Background = BasicColor.White;
            Color BackgroundColor = Color.White;
            Color PixelColor;
            BasicColor PixelBasicColor;

            if (UseBackgroundColor)
            {
                BackgroundColor = img.GetPixel(0, 0);
                //                Background = ParseColor(BackgroundColor);
            }

            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    if (UseBackgroundColor)
                    {
                        // Get a basic pixel color
                        PixelColor = img.GetPixel(x, y);
                        PixelBasicColor = ParseColor(PixelColor);
                        if (PixelColor.R != BackgroundColor.R ||
                            PixelColor.G != BackgroundColor.G ||
                            PixelColor.B != BackgroundColor.B)
                        {
                            // Return the height at this pixel location
                            imgArray[x, y] = _WallHeights[(byte)PixelBasicColor];
                            WallCount++;
                        }
                        else
                            imgArray[x, y] = 0.0f;
                    }
                    else
                    {
                        if (img.GetPixel(x, y).ToArgb() < WallCellColorThresh)
                        {
                            imgArray[x, y] = _state.DefaultHeight;
                            WallCount++;
                        }
                        else
                            imgArray[x, y] = 0.0f;
                    }
                }
            }
            Console.WriteLine("\nAdding grid world of size " + img.Width + " x " + img.Height + ". With " + WallCount + " wall blocks.");
            return imgArray;
        }


        // Adds a simple cube at a specified location in the maze grid
        void AddWall(int row, int col, float height, BasicColor color)
        {

            // TT Oct-2006 - Add an option to use a sphere instead of a cube
            if (_UseSphere[(byte)color])
            {
                AddBall((float)(-row * _state.GridSpacing), (float)(-col * _state.GridSpacing), height, color);
            }
            else
            {
                // Dimensions are in meters
                Vector3 dimensions =
                    new Vector3(_state.WallBoxSize * _state.GridSpacing,
                            height * _state.HeightScale,
                            _state.WallBoxSize * _state.GridSpacing);
                BoxShapeProperties cBoxShape = null;
                SingleShapeEntity box = null;

                // Create a simple box shape
                cBoxShape = new BoxShapeProperties(
                        _WallMasses[(byte)color], // mass in kilograms.
                        new Pose(),     // relative pose
                        dimensions);    // dimensions
                MaterialProperties bouncyMaterial = new MaterialProperties("Bouncy", 1.0f, 0.5f, 0.6f);
                //cBoxShape.Material = new MaterialProperties("gbox", 1.0f, 0.4f, 0.5f);
                cBoxShape.Material = bouncyMaterial;
                // Set the color of the box according to the bitmap image
                // or the specified color if no bitmap
                if (_WallTextures[(byte)color] == "")
                {
                    // TT - Changed for October CTP because DiffuseColor
                    // is a Vector4, but my WallColors are Vector3
                    //cBoxShape.DiffuseColor = _WallColors[(byte)color];
                    cBoxShape.DiffuseColor.X = (float)(_WallColors[(byte)color].X / 255.0);
                    cBoxShape.DiffuseColor.Y = (float)(_WallColors[(byte)color].Y / 255.0);
                    cBoxShape.DiffuseColor.Z = (float)(_WallColors[(byte)color].Z / 255.0);
                    cBoxShape.DiffuseColor.W = 0.5f;
                }
                else
                    cBoxShape.TextureFileName = _WallTextures[(byte)color];

                box = new SingleShapeEntity(new BoxShape(cBoxShape),
                    new Vector3(col * -_state.GridSpacing,
                            height * _state.HeightScale / 2,
                            -(row * _state.GridSpacing)));

                // Name the entity. All entities must have unique names
                box.State.Name = "wall_" + row + "_" + col;

                // Insert entity in simulation.  
                _simEnginePort.Insert(box);
            }

            BlockCounter++;
        }


        // Adds a long wall in the maze grid
        // Useful for reducing number of elements in simulation for better performance
        // TT -- Note that the existing code used height to refer to the
        // depth of the wall. Therefore the real height is called boxSize.
        void AddWall(int startRow, int startCol, int endRow, int endCol, float boxSize, BasicColor color)
        {
            int width = Math.Abs(endCol - startCol) + 1;
            int height = Math.Abs(endRow - startRow) + 1;

            float realWidth = (width * _state.GridSpacing) - (_state.GridSpacing - _state.WallBoxSize*_state.GridSpacing);
            float realHeight = (height * _state.GridSpacing) - (_state.GridSpacing - _state.WallBoxSize*_state.GridSpacing);

            //because the box is placed relative to the center of mass
            float widthOffset = (Math.Abs(endCol - startCol) * _state.GridSpacing) / 2;
            float heightOffset = -(Math.Abs(endRow - startRow) * _state.GridSpacing) / 2;

            if (_UseSphere[(byte)color])
            {
                AddBall((float)((-startRow * _state.GridSpacing) + heightOffset), 
                    (float)((-startCol * _state.GridSpacing) - widthOffset),
                    (float)(Math.Sqrt(realWidth * realWidth + realHeight * realHeight)),
                    color);
            }
            else
            {
                // This object is a wall (stretched cube)
                Vector3 dimensions =
                    new Vector3(realWidth, boxSize * _state.HeightScale, realHeight);
                        // Dimensions are in meters
                BoxShapeProperties cBoxShape = null;
                SingleShapeEntity box = null;

                cBoxShape = new BoxShapeProperties(
                        _WallMasses[(byte)color], // mass in kilograms.
                        new Pose(),     // relative pose
                        dimensions);    // dimensions
//                cBoxShape = new BoxShapeProperties(0, new Pose(), dimensions);
                // Walls have the same properties as the ground
                MaterialProperties bouncyMaterial = new MaterialProperties("Bouncy", 1.0f, 0.5f, 0.6f);
                //cBoxShape.Material = new MaterialProperties("gbox", 0.8f, 0.5f, 0.8f);
                cBoxShape.Material = bouncyMaterial;
                // Set the color of the box according to the bitmap image
                // or the specified color if no bitmap
                if (_WallTextures[(byte)color] == "")
                {
                    // TT - Changed for October CTP because DiffuseColor
                    // is a Vector4, but my WallColors are Vector3
                    //cBoxShape.DiffuseColor = _WallColors[(byte)color];
                    cBoxShape.DiffuseColor.X = (float)(_WallColors[(byte)color].X / 255.0);
                    cBoxShape.DiffuseColor.Y = (float)(_WallColors[(byte)color].Y / 255.0);
                    cBoxShape.DiffuseColor.Z = (float)(_WallColors[(byte)color].Z / 255.0);
                    cBoxShape.DiffuseColor.W = 0.5f;
                }
                else
                    cBoxShape.TextureFileName = _WallTextures[(byte)color];

                box = new SingleShapeEntity(new BoxShape(cBoxShape),
                    new Vector3((startCol * -_state.GridSpacing) - widthOffset,
                                boxSize * _state.HeightScale / 2,
                                -(startRow * _state.GridSpacing) + heightOffset)
                    );
                // Name the entity. All entities must have unique names
                box.State.Name = "wall_" + startRow + "_" + startCol;
                _simEnginePort.Insert(box);
            }

            BlockCounter++;
        }

        void AddBall(float row, float col, float height, BasicColor color)
        {
            float radius;
            radius = _state.SphereScale * height / 2.0f;

            MaterialProperties SlickMaterial = new MaterialProperties("Slick", 1.0f, 0.01f, 0.01f);
            //SphereShape ballShape = new SphereShape(new SphereShapeProperties(0.1f, new Pose(), 0.1f * _state.Scale));
            SphereShape ballShape = new SphereShape(
//                new SphereShapeProperties(0.1f, new Pose(), radius));
                new SphereShapeProperties(_WallMasses[(byte)color], new Pose(), radius));
            ballShape.State.Material = SlickMaterial;
            //ballShape.State.DiffuseColor = new Vector4(1, 0, 0, 1);

            if (_WallTextures[(byte)color] == "")
            {
                ballShape.State.DiffuseColor.X = (float)(_WallColors[(byte)color].X / 255.0);
                ballShape.State.DiffuseColor.Y = (float)(_WallColors[(byte)color].Y / 255.0);
                ballShape.State.DiffuseColor.Z = (float)(_WallColors[(byte)color].Z / 255.0);
                ballShape.State.DiffuseColor.W = 1.0f;
            }
            else
                ballShape.State.TextureFileName = _WallTextures[(byte)color];

            SingleShapeEntity Ball = new SingleShapeEntity(
                ballShape,
                new Vector3());

            //Ball.State.Name = "Ball";
            // Name the entity. All entities must have unique names
            Ball.State.Name = "Ball" + (int)row + (int)col;
            //ballShape.State.Name = "ball_" + (int)row + "_" + (int)col;

            //Ball.State.Pose.Position = new Vector3(col, row, 0.5f);
            Ball.State.Pose.Position = new Vector3(col, radius, row);
            SimulationEngine.GlobalInstancePort.Insert(Ball);

        }

        #endregion

        #region Handlers

        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ReplaceHandler(Replace replace)
        {
            _state = replace.Body;
            replace.ResponsePort.Post(DefaultReplaceResponseType.Instance);
            yield break;
        }

        #endregion
    }
}
