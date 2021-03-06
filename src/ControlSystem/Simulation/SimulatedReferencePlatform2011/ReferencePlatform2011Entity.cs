using System;
using System.ComponentModel;
using System.Linq;
using MathNet.Numerics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Xna.Framework;
using Quaternion = Microsoft.Robotics.PhysicalModel.Quaternion;
using Vector3 = Microsoft.Robotics.PhysicalModel.Vector3;

namespace Brumba.Simulation.SimulatedReferencePlatform2011
{
    /// <summary>
    /// Reference Platform variant of the motor base entity. It just specifies different physical properties in
    /// its custom constructor, otherwise uses the base class as is.
    /// </summary>
    [DataContract]
    public class ReferencePlatform2011Entity : VisualEntity
    {
        /// <summary>
        /// The Speed Delta
        /// </summary>
        private const float WheelAngularAcceleration = 5f;

        /// <summary>
        /// Thickness of platform
        /// </summary>
        protected const float ChassisThickness = 0.009525f;

        /// <summary>
        /// Depth from ground of the lowest platform
        /// </summary>
        protected const float ChassisDepthOffset = 0.03f;

        /// <summary>
        /// Depth from ground of the sensor platform
        /// </summary>
        protected const float ChassisSecondDepthOffset = 0.0978408f;

        /// <summary>
        /// Distance above platform that sensor point of origin floats
        /// </summary>
        protected const float SensorBoxHeightOffBase = 0.03f;

        /// <summary>
        /// Chassis mass in kilograms
        /// </summary>
        private const float Mass = 9f;

        private const float MaxSpeed = 1.5f;

        /// <summary>
        /// Distance from ground of chassis
        /// </summary>
        private float _chassisClearance;

        /// <summary>
        /// Mass of front wheels
        /// </summary>
        private const float _driveWheelMass = 0.01f;

        /// <summary>
        /// Radius of front wheels
        /// </summary>
        //_driveWheelRadius = 0.0799846f;
        private const float _driveWheelRadius = 0.0762f;

        /// <summary>
        /// Caster wheel radius
        /// </summary>
        private const float _casterWheelRadius = 0.0377952f;

        /// <summary>
        /// Front wheels width
        /// </summary>
        private const float _driveWheelWidth = 0.03175f;

        /// <summary>
        /// Caster wheel width
        /// </summary>
        private const float _casterWheelWidth = 0.05715f;

        /// <summary>
        /// Distance of the axle from the center of robot
        /// </summary>
        private const float _frontAxleDepthOffset = 0.0f;

        /// <summary>
        /// Gets or sets a value indicating whether the drive mechanism is enabled
        /// </summary>
        [DataMember, Description("Gets or sets a value indicating whether the drive mechanism is enabled.")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the scaling factor to apply to motor torque requests
        /// </summary>
        [DataMember, Description("Scaling factor to apply to motor torgue requests.")]
        public float CurrentToTorque { get; set; }

        [DataMember]
        public float FrictionTorque { get; set; }

        /// <summary>
        /// The current target velocity of the left wheel
        /// </summary>
        private float _leftWheelCurrent;

        /// <summary>
        /// The current target velocity of the right wheel
        /// </summary>
        private float _rightWheelCurrent;

        /// <summary>
        /// Gets or sets the right wheel child entity
        /// </summary>
        public WheelEntity RightWheel { get; private set; }

        /// <summary>
        /// Gets or sets the left wheel child entity
        /// </summary>
        public WheelEntity LeftWheel { get; private set; }

        /// <summary>
        /// Gets or sets the Kinect entity
        /// </summary>
        public KinectEntity Kinect { get; private set; }

        /// <summary>
        /// Gets or sets the Left Sonar Entity
        /// </summary>
        public SonarEntity LeftSonar { get; private set; }

        /// <summary>
        /// Gets or sets the Right Sonar Entity
        /// </summary>
        public SonarEntity RightSonar { get; private set; }

        /// <summary>
        /// Gets or sets the left IR entity
        /// </summary>
        public IREntity FrontLeftIr { get; private set; }

        /// <summary>
        /// Gets or sets the middle IR entity
        /// </summary>
        public IREntity FrontMiddleIr { get; private set; }

        /// <summary>
        /// Gets or sets the right IR entity
        /// </summary>
        public IREntity FrontRightIr { get; private set; }

        /// <summary>
        /// Chassis dimensions
        /// </summary>
        public readonly Vector3 _chassisDimensions = new Vector3(
                0.315f, // meters wide
                ChassisThickness,  // meters high
                0.315f);

        /// <summary>
        /// Body strut positions
        /// </summary>
        private Vector3[] _bodyStrutPositions =
         { 
             new Vector3(0.1651f,  0.0f,  0.09398f),
             new Vector3(-0.1651f, 0.0f,  0.09398f),
             new Vector3(0.1651f,  0.0f, -0.09398f),
             new Vector3(-0.1651f, 0.0f, -0.09398f)
         };

        /// <summary>
        /// Body strut dimensions
        /// </summary>
        private Vector3 _bodyStrutDimension = new Vector3(0.0127f, 0.136525f, 0.0127f);

        /// <summary>
        /// Kinect strut positions
        /// </summary>
        private Vector3[] _kinectStrutPositions = { new Vector3(0.0762f, 0.0f, 0.153289f), new Vector3(-0.0762f, 0.0f, 0.153289f) };

        /// <summary>
        /// Kinect strut dimensions
        /// </summary>
        private Vector3 _kinectStrutDimension = new Vector3(0.0127f, 0.3048f, 0.0127f);

        /// <summary>
        /// Kinect platform dimensions
        /// </summary>
        private Vector3 _kinectPlatformDimension = new Vector3(0.2032f, ChassisThickness, 0.0762f);

        /// <summary>
        /// Kinect platform position
        /// </summary>
        private Vector3 _kinectPlatformPosition = new Vector3(0.0f, 0.0f, 0.153289f);

        /// <summary>
        /// Sensor box dimensions
        /// </summary>
        private Vector3 _sensorBoxDimension = new Vector3(0.05f, 0.05f, 0.05f);

        /// <summary>
        /// Sonars are angled out 30 degrees of center. 
        /// </summary>
        private const double _sonarAngleOutRadians = 30 * Constants.Degree;

        /// <summary>
        /// Default constructor, used for creating the entity from an XML description
        /// </summary>
        public ReferencePlatform2011Entity()
        {
            CurrentToTorque = 1f;
            FrictionTorque = 0.03f;

            MeshScale = new Vector3(0.0254f, 0.0254f, 0.0254f);
        }

        /// <summary>
        /// Custom constructor for building model from hardcoded values. Used to create entity programmatically
        /// </summary>
        /// <param name="initialPos">The position to the place the reference platform at</param>
        public ReferencePlatform2011Entity(Vector3 initialPos)
            : this()
        {
            State.Pose.Position = initialPos;
        }

        /// <summary>
        /// Initializes the entity
        /// </summary>
        /// <param name="device">The graphics device</param>
        /// <param name="physicsEngine">The physics engine</param>
        public override void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            try
            {
                InitError = string.Empty;
                if (ChildCount == 0)
                    ProgrammaticallyBuildModel(device, physicsEngine);

                State.PhysicsPrimitives.Add(ConstructCasterWheelShape(
                    "front wheel", new Vector3(0, _casterWheelRadius + 0.001f, -_chassisDimensions.Z/2)));
                State.PhysicsPrimitives.Add(ConstructCasterWheelShape(
                    "rear wheel", new Vector3(0, _casterWheelRadius + 0.001f, _chassisDimensions.Z / 2)));

                //Adding batteries (in order to place main weight in the right point) makes MRDS fall in mc lrf test environment
                //on ref platform removal. Thats why weight is divided between caster wheels.
                //var batteries = new BoxShapeProperties("Batteries", 0.1f, new Pose(new Vector3(0, _casterWheelRadius, 0)),
                //    new Vector3(0.25f, 2 * ChassisThickness, 0.15f));
                //State.PhysicsPrimitives.Add(new BoxShape(batteries) { State = { EnableContactNotifications = false } });

                var accumulatedHeight = _chassisDimensions.Y + ChassisSecondDepthOffset + (_bodyStrutDimension.Y / 2.0f);

                State.PhysicsPrimitives.AddRange(_bodyStrutPositions.Select(sp =>
                    new BoxShape(new BoxShapeProperties("strut", 0.001f,
                        new Pose(new Vector3(sp.X, sp.Y + accumulatedHeight, sp.Z)),
                        _bodyStrutDimension))));

                accumulatedHeight += _bodyStrutDimension.Y / 2.0f;

                State.PhysicsPrimitives.Add(new BoxShape(new BoxShapeProperties("Top", 0.1f,
                    new Pose(new Vector3(0, accumulatedHeight, 0.0f)), _chassisDimensions)));

                State.PhysicsPrimitives.AddRange(_kinectStrutPositions.Select(ks => 
                    new BoxShape(new BoxShapeProperties("kstrut", 0.001f,
                        new Pose(new Vector3(ks.X, ks.Y + accumulatedHeight + (_kinectStrutDimension.Y/2.0f), ks.Z)),
                        _kinectStrutDimension))));

                accumulatedHeight += _kinectStrutDimension.Y;

                State.PhysicsPrimitives.Add(new BoxShape(new BoxShapeProperties("kplat", 0.001f,
                    new Pose(new Vector3(_kinectPlatformPosition.X,
                        _kinectPlatformPosition.Y + accumulatedHeight + (_kinectPlatformDimension.Y / 2.0f), _kinectPlatformPosition.Z)),
                    _kinectPlatformDimension)));

                // Add the various meshes for this platform
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "LowerDeck.obj"));
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "UpperDeck.obj"));
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "KinectStand.obj"));
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "Laptop.obj"));
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "Casters_Turned180.obj"));

                CreateAndInsertPhysicsEntity(physicsEngine);

                (LeftWheel = ConstructDriveWheel("left wheel", "Left_Tire.obj",
                    new Vector3(-_chassisDimensions.X / 2 + _driveWheelWidth / 2 - 0.01f, _driveWheelRadius, _frontAxleDepthOffset),
                    new Vector3(_chassisDimensions.X / 2, -_driveWheelRadius, 0))).
                    Initialize(device, physicsEngine);
                (RightWheel = ConstructDriveWheel("right wheel", "Right_Tire.obj",
                    new Vector3(+_chassisDimensions.X / 2 - _driveWheelWidth / 2 + 0.01f, _driveWheelRadius, _frontAxleDepthOffset),
                    new Vector3(-_chassisDimensions.X / 2, -_driveWheelRadius, 0))).
                    Initialize(device, physicsEngine);

                base.Initialize(device, physicsEngine);
            }
            catch (Exception ex)
            {
                // clean up
                if (PhysicsEntity != null)
                    PhysicsEngine.DeleteEntity(PhysicsEntity);

                HasBeenInitialized = false;
                InitError = ex.ToString();
            }
        }

        /// <summary>
        /// Self describes the reference platform
        /// </summary>
        /// <param name="device">The graphics device</param>
        /// <param name="physicsEngine">The physics engine</param>
        public void ProgrammaticallyBuildModel(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            // reference point for all shapes is the projection of
            // the center of mass onto the ground plane
            // (basically the spot under the center of mass, at Y = 0, or ground level)
            // NOTE: right/left is from the perspective of the robot, looking forward
            // NOTE: X = width of robot (right to left), Y = height, Z = length

            var sensorLevel = ChassisSecondDepthOffset + ChassisThickness + SensorBoxHeightOffBase;

            InsertEntity(FrontLeftIr = new IREntity(new Pose(new Vector3(-0.17f, sensorLevel, -0.1f)))
                { EntityState = { Name = EntityState.Name + "FrontLeftIR" } });

            InsertEntity(FrontRightIr = new IREntity(new Pose(new Vector3(0.17f, sensorLevel, -0.1f)))
                { EntityState = { Name = EntityState.Name + "FrontRightIR" } });

            InsertEntity(FrontMiddleIr = new IREntity(new Pose(new Vector3(0.0f, sensorLevel, -0.185f)))
                { EntityState = { Name = EntityState.Name + "FrontMiddleIR" } });

            InsertEntity(LeftSonar = new SonarEntity(new Pose(new Vector3(-0.09f, sensorLevel, -0.175f),
                Quaternion.FromAxisAngle(0f, 1f, 0f, (float)_sonarAngleOutRadians))) 
                { EntityState = { Name = EntityState.Name + "LeftSonar" } });

            InsertEntity(RightSonar = new SonarEntity(new Pose(new Vector3(0.09f, sensorLevel, -0.175f),
                Quaternion.FromAxisAngle(0f, 1f, 0f, -(float)_sonarAngleOutRadians)))
                { EntityState = { Name = EntityState.Name + "RightSonar" } });

            InsertEntity(Kinect = new KinectEntity(new Vector3(_kinectPlatformPosition.X,
                        _chassisDimensions.Y + ChassisSecondDepthOffset + (_bodyStrutDimension.Y / 2.0f) + _bodyStrutDimension.Y / 2.0f + _kinectStrutDimension.Y + _kinectPlatformDimension.Y,
                        _kinectPlatformPosition.Z), EntityState.Name));
        }

        SphereShape ConstructCasterWheelShape(string name, Vector3 position)
        {
            //Caster wheels bear the main weight of the robot!! Cause there was no way to place it in special battery shape,
            //read comment in Initialize
            return new SphereShape(new SphereShapeProperties(name, Mass / 2, new Pose(position), _casterWheelRadius))
            {
                State =
                {
                    Name = EntityState.Name + name,
                    Material = new MaterialProperties("qq", 0.0f, 0.0f, 0.0f)
                }
            };
        }

        WheelEntity ConstructDriveWheel(string name, string mesh, Vector3 position, Vector3 meshTranslation)
        {
            //Simulator simulates wheels in entirely nonphysical way: axle angular acceleration is proportional to motor torque
            //and inversly proportional to wheel mass (http://www.ogre3d.org/addonforums/viewtopic.php?f=6&t=7682&start=15).
            //Nothing more, for example the mass of the system, participates in the calculation.
            //That's why changing robot mass with given electrical current does not influence the robot acceleration.
            //And vica versa, wheel mass is not counted as part of a robot mass.
            //If all the mass of the robot is placed into wheels (and this is the case - ConstructCasterWheelShape), than
            //Reference platform becomes a peculiar corner case of diff drive platfrom. In this case wheels become independent,
            //that means that acceleration of each depends only on its velocity and inertia moment. That luckily coincides with
            //deficient wheel physical model of simulator and allows me to model it with general formula by setting their "mass"
            //(which is really inertia moment by dimension) accordingly.
            return new WheelEntity(new WheelShapeProperties(name, Mass / 2 * _driveWheelRadius * _driveWheelRadius, _driveWheelRadius)
                    {
                        InnerRadius = 0.7f * _driveWheelRadius,
                        LocalPose = new Pose(position),
                        Suspension = new SpringProperties(1e2f, 0, 0),
                        SuspensionTravel = 0.01f,
                        //Advanced = new ShapeAdvancedProperties {PhysicsCalculationPasses = 20}
                    })
            {
                State = { Name = EntityState.Name + name, Assets = { Mesh = mesh }, Pose = new Pose(position) },
                Parent = this,
                MeshTranslation = meshTranslation,
                MeshScale = new Vector3(0.0254f, 0.0254f, 0.0254f)
            };
        }

        public override void Dispose()
        {
            if (LeftWheel != null)
                LeftWheel.Dispose();

            if (RightWheel != null)
                RightWheel.Dispose();

            base.Dispose();
        }

        public override void Update(FrameUpdate update)
        {
            // update state for us and all the shapes that make up the rigid body
            PhysicsEntity.UpdateState(true);

            if (IsEnabled)
            {
                LeftWheel.Wheel.MotorTorque = -MotorTorque(_leftWheelCurrent, -LeftWheel.Wheel.AxleSpeed);
                RightWheel.Wheel.MotorTorque = -MotorTorque(_rightWheelCurrent, -RightWheel.Wheel.AxleSpeed);
            }

            // update entities in fields
            LeftWheel.Update(update);
            RightWheel.Update(update);

            // sim engine will update children
            base.Update(update);
        }

        float MotorTorque(float current, float angVelocity)
        {
            //With increased wheel mass friction torque starts being hard to ignore
            return CurrentToTorque * current - (CurrentToTorque - FrictionTorque) / MaxSpeed * (_driveWheelRadius * angVelocity);
            //return CurrentToTorque * current - CurrentToTorque / MaxSpeed * (_driveWheelRadius * angVelocity);
        }

        public override void Render(RenderMode renderMode, MatrixTransforms transforms, CameraEntity currentCamera)
        {
            var entityEffect = LeftWheel.Effect;
            if (currentCamera.LensEffect != null)
            {
                LeftWheel.Effect = currentCamera.LensEffect;
                RightWheel.Effect = currentCamera.LensEffect;
            }

            LeftWheel.Render(renderMode, transforms, currentCamera);
            RightWheel.Render(renderMode, transforms, currentCamera);

            LeftWheel.Effect = entityEffect;
            RightWheel.Effect = entityEffect;

            base.Render(renderMode, transforms, currentCamera);
        }

        public void SetMotorCurrent(float leftWheelCurrent, float rightWheelCurrent)
        {
            _leftWheelCurrent = leftWheelCurrent;
            _rightWheelCurrent = rightWheelCurrent;
        }
    }
}