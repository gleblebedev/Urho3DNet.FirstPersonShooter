namespace Urho3DNet.FirstPersonShooter
{
    [ObjectFactory]
    public class ClassicFpsCharacter : LogicComponent
    {
        private float _yaw;
        private float _pitch;
        private float _roll;
        private RigidBody _rigidBody;
        private CollisionShape _collisionShape;
        private KinematicCharacterController _kinematicCharacterController;
        private Node _cameraPivot;
        private Vector3 _forwardMovementDir;
        private Vector3 _rightMovementDir;
        private float _sinceCanJump;
        private bool _isJumping;
        private float _gravity = 25;
        private float _physicsStep = 1.0f / 60.0f;
        private bool _jumpPressed;
        private bool _jumpRequested;
        private Vector3 _lastKnownPosition;
        private Vector3 _currentVelocity;

        public ClassicFpsCharacter(Context context) : base(context)
        {
            UpdateEventMask = UpdateEvent.UseUpdate | UpdateEvent.UseFixedupdate;
        }

        public float Forward { get; set; }
        public float Backward { get; set; }
        public float Left { get; set; }
        public float Right { get; set; }
        public float MaxSpeed { get; set; } = 10;
        public float LateJumpDelay { get; set; } = 0.10f;

        public float Gravity
        {
            get => _gravity;
            set
            {
                _gravity = value;
                if (_kinematicCharacterController != null)
                    _kinematicCharacterController.SetGravity(new Vector3(0, -_gravity));
            }
        }

        public Camera Camera { get; private set; }

        public bool Jump
        {
            get => _jumpPressed;
            set
            {
                if (_jumpPressed != value)
                {
                    _jumpPressed = value;
                    _jumpRequested = value;
                }
            }
        }

        public override void FixedUpdate(float timeStep)
        {
            MoveCharacter(timeStep);

            if (_kinematicCharacterController.CanJump())
            {
                _sinceCanJump = 0;
                _isJumping = false;
            }
            else
            {
                _sinceCanJump += timeStep;
            }

            if (_jumpRequested)
                if (!_isJumping && _sinceCanJump < LateJumpDelay)
                {
                    _kinematicCharacterController.Jump();
                    _isJumping = true;
                    _jumpRequested = false;
                }
        }

        public override void Update(float timeStep)
        {
            //else
            //{
            //    Debug.WriteLine("Missed by "+_sinceCanJump);
            //}

            base.Update(timeStep);
        }

        public void Rotate(float yaw, float pitch, float roll)
        {
            if (yaw == 0 && pitch == 0 && roll == 0)
                return;
            _yaw += yaw;
            if (_yaw < 0)
                _yaw += 360.0f;
            if (_yaw > 360.0f)
                _yaw -= 360.0f;
            _pitch += pitch;
            if (_pitch < -89f)
                _pitch = -89f;
            if (_pitch > 89f)
                _pitch = 89f;
            _roll += roll;
            if (_roll < -89f)
                _roll = -89f;
            if (_roll > 89f)
                _roll = 89f;
            UpdateCamera();
        }

        protected override void Dispose(bool disposing)
        {
            Camera?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnNodeSet(Node node)
        {
            if (node != null)
            {
                var pw = node.GetOrCreateComponent<PhysicsWorld>();
                _physicsStep = 1.0f / pw.Fps;

                _rigidBody = node.GetComponent<RigidBody>();
                if (_rigidBody == null)
                {
                    _rigidBody = node.CreateComponent<RigidBody>();
                    _rigidBody.CollisionLayer = 1;
                    _rigidBody.IsKinematic = true;
                    _rigidBody.IsTrigger = true;
                    _rigidBody.AngularFactor = Vector3.Zero;
                    _rigidBody.CollisionEventMode = CollisionEventMode.CollisionAlways;
                }


                _collisionShape = node.GetComponent<CollisionShape>();
                if (_collisionShape == null)
                {
                    _collisionShape = node.CreateComponent<CollisionShape>();
                    _collisionShape.SetCapsule(1.0f, 1.8f, new Vector3(0, 0.9f));
                }

                _kinematicCharacterController = node.GetComponent<KinematicCharacterController>();
                if (_kinematicCharacterController == null)
                {
                    _kinematicCharacterController = node.CreateComponent<KinematicCharacterController>();
                    _kinematicCharacterController.SetGravity(new Vector3(0, -_gravity));
                    _kinematicCharacterController.SetJumpSpeed(8);
                }

                Camera = node.GetComponent<Camera>(true);
                if (Camera != null)
                {
                    _cameraPivot = Camera.Node;
                }
                else
                {
                    _cameraPivot = node.CreateChild("CameraPivot");
                    _cameraPivot.Position = new Vector3(0, 1.6f);
                    Camera = _cameraPivot.CreateComponent<Camera>();
                }

                _lastKnownPosition = Node.WorldPosition;

                UpdateCamera();
            }

            base.OnNodeSet(node);
        }

        private void MoveCharacter(float timeStep)
        {
            var newPosition = Node.WorldPosition;
            var currentVelocity = (newPosition - _lastKnownPosition) / timeStep;
            currentVelocity.Y = 0;
            //Debug.WriteLine(currentVelocity);
            _lastKnownPosition = newPosition;

            //var currentVelocity = _rigidBody.LinearVelocity;
            //currentVelocity.Y = 0;

            //var currentVelocity = _currentVelocity;

            var wishdir = Vector3.Zero;
            wishdir += _forwardMovementDir * (Forward - Backward);
            wishdir += _rightMovementDir * (Right - Left);
            var onGround = _kinematicCharacterController.OnGround();

            var wishspeed = wishdir.Length;
            if (wishspeed < 1e-6f)
            {
                wishspeed = 0;
                wishdir = Vector3.Zero;
            }
            else
            {
                wishdir *= 1.0f / wishspeed;
            }

            if (wishspeed > 1.0f)
                wishspeed = 1.0f;
            wishspeed = MaxSpeed * wishspeed;

            var currentSpeed = currentVelocity.DotProduct(wishdir);
            var deltaVelocity = wishspeed * wishdir - currentVelocity;
            var deltaLength = deltaVelocity.Length;
            float acceleration;
            if (wishspeed > currentSpeed)
                acceleration = onGround ? 100.0f : 4.0f;
            else
                acceleration = onGround ? 30.0f : 4.0f;

            var maxSpeedDiff = acceleration * timeStep;
            if (maxSpeedDiff >= deltaLength)
                currentVelocity += deltaVelocity;
            else
                currentVelocity += deltaVelocity * (maxSpeedDiff / deltaLength);

            //if (wishdir != Vector3.Zero)
            //{
            //    //Debug.WriteLine("Walk direction" + movement);
            //}

            _kinematicCharacterController.SetWalkDirection(currentVelocity * _physicsStep);
            _currentVelocity = currentVelocity;
        }

        private void UpdateCamera()
        {
            _cameraPivot.Rotation = new Quaternion(new Vector3(_pitch, _yaw, _roll));
            var r = new Quaternion(new Vector3(0, _yaw));
            _forwardMovementDir = r * Vector3.Forward;
            _rightMovementDir = r * Vector3.Right;

            //Debug.WriteLine("Forward "+_forwardMovementDir);
        }
    }
}