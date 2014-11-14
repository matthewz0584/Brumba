using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;

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

        #region State

        /// <summary>
        /// Left front wheel position
        /// </summary>
        private Vector3 _leftFrontWheelPosition;
        
        /// <summary>
        /// Right front wheel position
        /// </summary>
        private Vector3 _rightFrontWheelPosition;
        
        /// <summary>
        /// Caster wheel position
        /// </summary>
        private Vector3 _casterWheelPosition;

        /// <summary>
        /// Distance from ground of chassis
        /// </summary>
        private float _chassisClearance;
        
        /// <summary>
        /// Mass of front wheels
        /// </summary>
        private const float _frontWheelMass = 0.01f;
        
        /// <summary>
        /// Radius of front wheels
        /// </summary>
        //_frontWheelRadius = 0.0799846f;
        private const float _frontWheelRadius = 0.0762f;
        
        /// <summary>
        /// Caster wheel radius
        /// </summary>
        private const float _chassisWheelRadius = 0.0377952f;
        
        /// <summary>
        /// Front wheels width
        /// </summary>
        private const float _frontWheelWidth = 0.03175f;
        
        /// <summary>
        /// Caster wheel width
        /// </summary>
        private const float _casterWheelWidth = 0.05715f;
        
        /// <summary>
        /// Distance of the axle from the center of robot
        /// </summary>
        private const float _frontAxleDepthOffset = 0.0f;

        /// <summary>
        /// Gets or sets the mesh file for front wheels
        /// </summary>
        [Description("Gets or sets the mesh file for front wheels")]
        public string WheelMesh { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the drive mechanism is enabled
        /// </summary>
        [DataMember, Description("Gets or sets a value indicating whether the drive mechanism is enabled.")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the scaling factor to apply to motor torque requests
        /// </summary>
        [DataMember, Description("Scaling factor to apply to motor torgue requests.")]
        public float MotorTorqueScaling { get; set; }

        /// <summary>
        /// Gets the current heading, in radians, of robot base
        /// </summary>
        public float CurrentHeading
        {
            get
            {
                // return the axis angle of the quaternion
                var euler = UIMath.QuaternionToEuler(State.Pose.Orientation);
                return Microsoft.Xna.Framework.MathHelper.ToRadians(euler.Y); // heading is the rotation about the Y axis.
            }
        }

        /// <summary>
        /// The current target velocity of the left wheel
        /// </summary>
        private float _leftTargetVelocity;
        
        /// <summary>
        /// The current target velocity of the right wheel
        /// </summary>
        private float _rightTargetVelocity;

        /// <summary>
        /// Gets or sets the right wheel child entity
        /// </summary>
        public WheelEntity RightWheel { get; private set; }

        /// <summary>
        /// Gets or sets the left wheel child entity
        /// </summary>
        public WheelEntity LeftWheel { get; private set; }

        /// <summary>
        /// Gets or sets the Front wheel physics shape
        /// </summary>
        [DataMember, Description("Front wheel physics shape.")]
        public SphereShape FrontWheelShape { get; set; }

        /// <summary>
        /// Gets or sets the Rear wheel physics shape
        /// </summary>
        [DataMember, Description("rear wheel physics shape.")]
        public SphereShape RearWheelShape { get; set; }

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

        #endregion

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
        /// Left IR position
        /// </summary>
        private Vector3 _leftIrBoxPosition = new Vector3(-0.17f, ChassisSecondDepthOffset + ChassisThickness + SensorBoxHeightOffBase, -0.1f);
        
        /// <summary>
        /// Middle IR position
        /// </summary>
        private Vector3 _middleIrBoxPosition = new Vector3(0.0f, ChassisSecondDepthOffset + ChassisThickness + SensorBoxHeightOffBase, -0.185f);
        
        /// <summary>
        /// Right IR position
        /// </summary>
        private Vector3 _rightIrBoxPosition = new Vector3(0.17f, ChassisSecondDepthOffset + ChassisThickness + SensorBoxHeightOffBase, -0.1f);

        /// <summary>
        /// Left sonar position
        /// </summary>
        private Vector3 _leftSonarBoxPosition = new Vector3(-0.09f, ChassisSecondDepthOffset + ChassisThickness + SensorBoxHeightOffBase, -0.175f);

        /// <summary>
        /// Right sonar position
        /// </summary>
        private Vector3 _rightSonarBoxPosition = new Vector3(0.09f, ChassisSecondDepthOffset + ChassisThickness + SensorBoxHeightOffBase, -0.175f);

        /// <summary>
        /// Sonars are angled out 30 degrees left of center. 
        /// </summary>
        private const double LeftSonarAngleOutRadians = 30 / (180 / Math.PI);

        /// <summary>
        /// Left sonar orientation. 
        /// </summary>
        private Quaternion _leftSonarOrientation = Quaternion.FromAxisAngle(0f, 1f, 0f, (float)LeftSonarAngleOutRadians);

        /// <summary>
        /// Sonars are angled out 30 degrees right of center. 
        /// </summary>
        private const double RightSonarAngleOutRadians = -30 / (180 / Math.PI);

        /// <summary>
        /// Right sonar orientation. 
        /// </summary>
        private Quaternion _rightSonarOrientation = Quaternion.FromAxisAngle(0f, 1f, 0f, (float)RightSonarAngleOutRadians);

        /// <summary>
        /// Default constructor, used for creating the entity from an XML description
        /// </summary>
        public ReferencePlatform2011Entity()
        {
            InitializePhysicalAttributes();
        }

        /// <summary>
        /// Custom constructor for building model from hardcoded values. Used to create entity programmatically
        /// </summary>
        /// <param name="initialPos">The position to the place the reference platform at</param>
        public ReferencePlatform2011Entity(Vector3 initialPos)
            :this()
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
                if (FrontWheelShape == null)
                    ProgrammaticallyBuildModel(device, physicsEngine);

                State.PhysicsPrimitives.Add(FrontWheelShape);
                State.PhysicsPrimitives.Add(RearWheelShape);

                var bottomDesc = new BoxShapeProperties("Batteries", Mass, new Pose(new Vector3(0, _chassisWheelRadius, 0)),
                    new Vector3(0.25f, ChassisThickness, 0.15f));
                State.PhysicsPrimitives.Add(new BoxShape(bottomDesc));

                float accumulatedHeight = _chassisDimensions.Y + ChassisSecondDepthOffset + (_bodyStrutDimension.Y / 2.0f);
                foreach (Vector3 strutPosition in _bodyStrutPositions)
                {
                    var strutDesc = new BoxShapeProperties(
                        "strut",
                        0.001f,
                        new Pose(
                            new Vector3(
                                strutPosition.X,
                                strutPosition.Y + accumulatedHeight,
                                strutPosition.Z)),
                        _bodyStrutDimension);
                    var strutShape = new BoxShape(strutDesc);
                    State.PhysicsPrimitives.Add(strutShape);
                }
                accumulatedHeight += _bodyStrutDimension.Y / 2.0f;
                var topDesc =
                    new BoxShapeProperties(
                        "Top",
                        0.1f,
                        new Pose(
                            new Vector3(
                                0,
                                accumulatedHeight, // chassis is off the ground
                                0.0f)), // minor offset in the z/depth axis
                        _chassisDimensions);

                var topShape = new BoxShape(topDesc);
                State.PhysicsPrimitives.Add(topShape);

                foreach (Vector3 kinectStrut in _kinectStrutPositions)
                {
                    var strutDesc =
                        new BoxShapeProperties(
                            "kstrut",
                            0.001f,
                            new Pose(
                                new Vector3(
                                    kinectStrut.X,
                                    kinectStrut.Y + accumulatedHeight + (_kinectStrutDimension.Y / 2.0f),
                                    kinectStrut.Z)),
                            _kinectStrutDimension);
                    var strutShape = new BoxShape(strutDesc);
                    State.PhysicsPrimitives.Add(strutShape);
                }

                accumulatedHeight += _kinectStrutDimension.Y;

                var kinectPlatformDesc =
                    new BoxShapeProperties(
                        "kplat",
                        0.001f,
                        new Pose(
                            new Vector3(
                                _kinectPlatformPosition.X,
                                _kinectPlatformPosition.Y + accumulatedHeight + (_kinectPlatformDimension.Y / 2.0f),
                                _kinectPlatformPosition.Z)),
                        _kinectPlatformDimension);
                var kinectPlatformShape = new BoxShape(kinectPlatformDesc);
                State.PhysicsPrimitives.Add(kinectPlatformShape);

                // Add the various meshes for this platform
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "LowerDeck.obj"));
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "UpperDeck.obj"));
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "KinectStand.obj"));
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "Laptop.obj"));
                Meshes.Add(SimulationEngine.ResourceCache.CreateMeshFromFile(device, "Casters_Turned180.obj"));

                CreateAndInsertPhysicsEntity(physicsEngine);

                // Set the parent entity for the wheel entities, clear any local rotation
                // on the wheel shape so that the wheel contact is always in the -Y direction.
                LeftWheel.Parent = this;
                LeftWheel.Wheel.State.LocalPose.Orientation = new Quaternion(0, 0, 0, 1);
                RightWheel.Parent = this;
                RightWheel.Wheel.State.LocalPose.Orientation = new Quaternion(0, 0, 0, 1);
                LeftWheel.Initialize(device, physicsEngine);
                RightWheel.Initialize(device, PhysicsEngine);

                base.Initialize(device, physicsEngine);

                IsEnabled = true;
            }
            catch (Exception ex)
            {
                // clean up
                if (PhysicsEntity != null)
                {
                    PhysicsEngine.DeleteEntity(PhysicsEntity);
                }

                HasBeenInitialized = false;
                InitError = ex.ToString();
            }
        }
        
        /// <summary>
        /// Sets the various dimensions of our physical components
        /// </summary>
        private void InitializePhysicalAttributes()
        {
            //State.MassDensity.Mass = Mass;
            //State.MassDensity.CenterOfMass = new Pose(new Vector3(0, _chassisClearance, 0));

            // reference point for all shapes is the projection of
            // the center of mass onto the ground plane
            // (basically the spot under the center of mass, at Y = 0, or ground level)
            // NOTE: right/left is from the perspective of the robot, looking forward
            // NOTE: X = width of robot (right to left), Y = height, Z = length

            // rear wheel is also called the caster
            _casterWheelPosition = new Vector3(
                0, // center of chassis
                _chassisWheelRadius, // distance from ground
                _chassisDimensions.Z / 2); // at the rear of the robot

            _rightFrontWheelPosition = new Vector3(
                +(_chassisDimensions.X / 2) - (_frontWheelWidth / 2) + 0.01f, // left of center
                _frontWheelRadius, // distance from ground of axle
                _frontAxleDepthOffset); // distance from center, on the z-axis

            _leftFrontWheelPosition = new Vector3(
                -(_chassisDimensions.X / 2) + (_frontWheelWidth / 2) - 0.01f, // right of center
                _frontWheelRadius, // distance from ground of axle
                _frontAxleDepthOffset); // distance from center, on the z-axis

            MotorTorqueScaling = 20;

            MeshScale = new Vector3(0.0254f, 0.0254f, 0.0254f);

            ConstructWheels();

            // Add the wheel meshes separately
            LeftWheel.EntityState.Assets.Mesh = "Left_Tire.obj";
            LeftWheel.MeshScale = new Vector3(0.0254f, 0.0254f, 0.0254f);
            LeftWheel.MeshTranslation = new Vector3(_chassisDimensions.X / 2, -_frontWheelRadius, 0);
            RightWheel.EntityState.Assets.Mesh = "Right_Tire.obj";
            RightWheel.MeshScale = new Vector3(0.0254f, 0.0254f, 0.0254f);

            // Override the 180 degree rotation (that the Diff Drive applies) because we have separate wheel meshes
            RightWheel.MeshRotation = new Vector3(0, 0, 0);
            RightWheel.MeshTranslation = new Vector3(-_chassisDimensions.X / 2, -_frontWheelRadius, 0);
        }

        /// <summary>
        /// Self describes the reference platform
        /// </summary>
        /// <param name="device">The graphics device</param>
        /// <param name="physicsEngine">The physics engine</param>
        public void ProgrammaticallyBuildModel(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            // add a front wheel
            var frontWheelPosition = new Vector3(
                0, // center of chassis
                _chassisWheelRadius, // distance from ground
                (-_chassisDimensions.Z / 2) + 0.000f); // at the front of the robot

                FrontWheelShape = new SphereShape(
                    new SphereShapeProperties("front wheel", 0.001f, new Pose(frontWheelPosition), _chassisWheelRadius))
                    {
                        State =
                            {
                                Name = EntityState.Name + "FrontWheel",
                                Material = new MaterialProperties("small friction with anisotropy", 0.5f, 0.5f, 1)
                                    {
                                        Advanced =
                                            new MaterialAdvancedProperties
                                                {
                                                    AnisotropicDynamicFriction = 0.3f,
                                                    AnisotropicStaticFriction = 0.4f,
                                                    AnisotropyDirection = new Vector3(0, 0, 1)
                                                }
                                    }
                            }
                    };

                // a fixed caster wheel has high friction when moving laterally, but low friction when it moves along the
                // body axis its aligned with. We use anisotropic friction to model this

            // add a rear wheel
            var rearWheelPosition = new Vector3(
                0, // center of chassis
                _chassisWheelRadius, // distance from ground
                (_chassisDimensions.Z / 2) + 0.000f); // at the back of the robot

                RearWheelShape = new SphereShape(
                    new SphereShapeProperties(
                        "rear wheel",
                        0.001f,
                        new Pose(rearWheelPosition),
                        _chassisWheelRadius))
                    {
                        State =
                            {
                                Name = EntityState.Name + "RearWheel",
                                Material =
                                    new MaterialProperties("small friction with anisotropy", 0.5f, 0.5f, 1)
                                        {
                                            Advanced =
                                                new MaterialAdvancedProperties
                                                    {
                                                        AnisotropicDynamicFriction = 0.3f,
                                                        AnisotropicStaticFriction = 0.4f,
                                                        AnisotropyDirection = new Vector3(0, 0, 1)
                                                    }
                                        }
                            }
                    };

                // a fixed caster wheel has high friction when moving laterally, but low friction when it moves along the
                // body axis its aligned with. We use anisotropic friction to model this

            float accumulatedHeight = _chassisDimensions.Y + ChassisSecondDepthOffset + (_bodyStrutDimension.Y / 2.0f);

            accumulatedHeight += _bodyStrutDimension.Y / 2.0f;

                FrontLeftIr = new IREntity(new Pose(_leftIrBoxPosition))
                    {
                        EntityState = {Name = EntityState.Name + "FrontLeftIR"}
                    };
                InsertEntity(FrontLeftIr);

            FrontRightIr = new IREntity(new Pose(_rightIrBoxPosition))
                    {
                        EntityState = {Name = EntityState.Name + "FrontRightIR"}
                    };
                InsertEntity(FrontRightIr);

            FrontMiddleIr = new IREntity(new Pose(_middleIrBoxPosition))
                    {
                        EntityState = {Name = EntityState.Name + "FrontMiddleIR"}
                    };
                InsertEntity(FrontMiddleIr);

            LeftSonar = new SonarEntity(
                    new Pose(_leftSonarBoxPosition, _leftSonarOrientation))
                    {
                        EntityState = {Name = EntityState.Name + "LeftSonar"}
                    };
                InsertEntity(LeftSonar);

            RightSonar = new SonarEntity(
                    new Pose(
                        _rightSonarBoxPosition,
                        _rightSonarOrientation)) {EntityState = {Name = EntityState.Name + "RightSonar"}};
                InsertEntity(RightSonar);

            accumulatedHeight += _kinectStrutDimension.Y;

            accumulatedHeight += _kinectPlatformDimension.Y;

            Kinect = new KinectEntity(new Vector3(_kinectPlatformPosition.X, accumulatedHeight, _kinectPlatformPosition.Z), EntityState.Name);
            InsertEntity(Kinect);
        }

        /// <summary>
        /// Constructs the wheel components
        /// </summary>
        protected void ConstructWheels()
        {
            // front left wheel
            var w = new WheelShapeProperties("front left wheel", _frontWheelMass, _frontWheelRadius);

            // Set this flag on both wheels if you want to use axle speed instead of torque
            w.Flags |= WheelShapeBehavior.OverrideAxleSpeed;
            w.InnerRadius = 0.7f * w.Radius;
            w.LocalPose = new Pose(_leftFrontWheelPosition);
            LeftWheel = new WheelEntity(w)
                {
                    State = {Name = EntityState.Name + "LeftWheel", Assets = {Mesh = WheelMesh}},
                    Parent = this
                };

            // front right wheel
            w = new WheelShapeProperties("front right wheel", _frontWheelMass, _frontWheelRadius);
            w.Flags |= WheelShapeBehavior.OverrideAxleSpeed;
            w.InnerRadius = 0.7f * w.Radius;
            w.LocalPose = new Pose(_rightFrontWheelPosition);
            RightWheel = new WheelEntity(w)
                {
                    State = {Name = State.Name + "RightWheel", Assets = {Mesh = WheelMesh}},
                    MeshRotation = new Vector3(0, 180, 0),
                    Parent = this
                };
        }

        /// <summary>
        /// Special dispose to handle embedded entities
        /// </summary>
        public override void Dispose()
        {
            if (LeftWheel != null)
                LeftWheel.Dispose();

            if (RightWheel != null)
                RightWheel.Dispose();

            base.Dispose();
        }

        /// <summary>
        /// Updates pose for our entity. We override default implementation
        /// since we control our own rendering when no file mesh is supplied, which means
        /// we dont need world transform updates
        /// </summary>
        /// <param name="update">The frame update message</param>
        public override void Update(FrameUpdate update)
        {
            // update state for us and all the shapes that make up the rigid body
            PhysicsEntity.UpdateState(true);

            if (IsEnabled)
            {
                UpdateAxleSpeed(LeftWheel.Wheel, _leftTargetVelocity, (float) update.ElapsedTime);
                UpdateAxleSpeed(RightWheel.Wheel, _rightTargetVelocity, (float) update.ElapsedTime);
            }
            else
            {
                LeftWheel.Wheel.AxleSpeed = 0;
                RightWheel.Wheel.AxleSpeed = 0;
            }

            // update entities in fields
            LeftWheel.Update(update);
            RightWheel.Update(update);

            // sim engine will update children
            base.Update(update);
        }

        void UpdateAxleSpeed(PhysicsWheel wheel, float targetAxleSpeed, float elapsedTime)
        {
            var axleSpeedDelta = targetAxleSpeed - (-wheel.AxleSpeed);
            if (Math.Abs(axleSpeedDelta) <= 0.1)
                return;
            wheel.AxleSpeed += - WheelAngularAcceleration * elapsedTime * Math.Sign(axleSpeedDelta);
        }

        /// <summary>
        /// Render entities stored as fields
        /// </summary>
        /// <param name="renderMode">The render mode</param>
        /// <param name="transforms">The transforms</param>
        /// <param name="currentCamera">The current camera</param>
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

        /// <summary>
        /// Sets motor torque on the active wheels
        /// </summary>
        /// <param name="leftWheelTorque">The left wheel torque</param>
        /// <param name="rightWheelTorque">The right wheel torque</param>
        public void SetMotorTorque(float leftWheelTorque, float rightWheelTorque)
        {
            if (LeftWheel == null || RightWheel == null)
                return;

            _leftTargetVelocity = leftWheelTorque * MotorTorqueScaling;
            _rightTargetVelocity = rightWheelTorque * MotorTorqueScaling;
        }

        /// <summary>
        /// Sets angular velocity on the wheels
        /// </summary>
        /// <param name="left">Velocity for left wheel</param>
        /// <param name="right">Velocity for right wheel</param>
        public void SetVelocity(float left, float right)
        {
            if (LeftWheel == null || RightWheel == null)
                return;

            _leftTargetVelocity = left / LeftWheel.Wheel.State.Radius;
            _rightTargetVelocity = right / RightWheel.Wheel.State.Radius;
        }
        
        /// <summary>
        /// Adds a notification port to the list of subscriptions that get notified when the bumper shapes
        /// collide in the physics world
        /// </summary>
        /// <param name="notificationTarget">The target of the notification</param>
        public void Subscribe(Port<EntityContactNotification> notificationTarget)
        {
            PhysicsEntity.SubscribeForContacts(notificationTarget);
        }
    }
}