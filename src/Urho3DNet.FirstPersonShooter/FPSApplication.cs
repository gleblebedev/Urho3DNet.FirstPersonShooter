using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Urho3DNet.FirstPersonShooter
{
    public class FPSApplication : Application
    {
        /// Scene.
        private readonly SharedPtr<Scene> _scene = new SharedPtr<Scene>(null);

        /// Camera scene node.
        private readonly SharedPtr<Viewport> _viewport = new SharedPtr<Viewport>(null);

        private Input _input;
        private Node _level;
        private Node _player;
        private readonly float _mouseSensetivity = 0.22f;
        private ClassicFpsCharacter _character;
        private List<Node> _spawnPoints;

        public FPSApplication(Context context) : base(context)
        {
            context.RegisterFactory<ClassicFpsCharacter>();
            SubscribeToEvent(E.LogMessage, OnMessage);
        }

        private void OnMessage(VariantMap obj)
        {
            var level = (LogLevel)obj[E.LogMessage.Level].Int;
            var message = obj[E.LogMessage.Message].String;
            switch (level)
            {
                case LogLevel.LogError:
#if DEBUG
                    if (!message.Contains("Failed to create input layout"))
                        throw new ApplicationException(message);
#endif
                    Trace.WriteLine(message);
                    break;
                default:
                    Trace.WriteLine(message);
                    break;
            }
        }
        protected Viewport Viewport
        {
            get => _viewport?.Value;
            set => _viewport.Value = value;
        }

        protected Scene Scene
        {
            get => _scene?.Value;
            set => _scene.Value = value;
        }

        public override void Setup()
        {
            var windowed = Debugger.IsAttached;
            EngineParameters[Urho3D.EpFullScreen] = !windowed;
            if (windowed) EngineParameters[Urho3D.EpWindowResizable] = true;
            EngineParameters[Urho3D.EpWindowTitle] = "First Person Shooter Demo";
            EngineParameters[Urho3D.EpFrameLimiter] = true;
            EngineParameters[Urho3D.EpVsync] = true;
            EngineParameters[Urho3D.EpRenderPath] = "RenderPaths/Desktop.xml";
        }


        public override void Start()
        {
            // Execute base class startup
            base.Start();

            _input = Context.Input;

            //if (touchEnabled_)
            //    touch_ = new Touch(Context, TOUCH_SENSITIVITY);

            // Create static scene content
            CreateScene();


            //// Create the UI content
            //CreateInstructions();

            // Subscribe to necessary events
            SubscribeToEvents();

            // Set the mouse mode to use in the sample
            InitMouseMode(MouseMode.MmRelative);
        }

        public override void Stop()
        {
            _viewport.Dispose();
            _scene.Dispose();
            base.Stop();
        }

        protected void InitMouseMode(MouseMode mode)
        {
            var input = Context.Input;

            var console = GetSubsystem<Console>();

            Context.Input.SetMouseMode(mode);
            if (console != null && console.IsVisible)
                Context.Input.SetMouseMode(MouseMode.MmAbsolute, true);
        }

        private void SubscribeToEvents()
        {
            // Subscribe to Update event for setting the character controls before physics simulation
            SubscribeToEvent(E.Update, HandleUpdate);

            // Unsubscribe the SceneUpdate event from base class as the camera node is being controlled in HandlePostUpdate() in this sample
            UnsubscribeFromEvent(E.SceneUpdate);

            SubscribeToEvent(E.KeyDown, HandleKeyDown);
            SubscribeToEvent(E.KeyUp, HandleKeyUp);
            SubscribeToEvent(E.MouseMove, HandleMouseMove);
        }

        private void HandleUpdate(VariantMap args)
        {
            var timestep = args[E.Update.TimeStep].Float;


            var mouseMove = _input.MouseMove;
            _character.Rotate(mouseMove.X * _mouseSensetivity, mouseMove.Y * _mouseSensetivity, 0);
        }

        private void HandleMouseMove(VariantMap obj)
        {
        }

        private void HandleKeyUp(VariantMap obj)
        {
            var keycode = (Key) obj[E.KeyUp.Key].Int;

            switch (keycode)
            {
                case Key.KeyUp:
                case Key.KeyW:
                    _character.Forward = 0.0f;
                    break;
                case Key.KeyDown:
                case Key.KeyS:
                    _character.Backward = 0.0f;
                    break;
                case Key.KeyLeft:
                case Key.KeyA:
                    _character.Left = 0.0f;
                    break;
                case Key.KeyRight:
                case Key.KeyD:
                    _character.Right = 0.0f;
                    break;
                case Key.KeySpace:
                    _character.Jump = false;
                    break;
            }
        }

        private void HandleKeyDown(VariantMap obj)
        {
            var keycode = (Key) obj[E.KeyDown.Key].Int;

            switch (keycode)
            {
                case Key.KeyUp:
                case Key.KeyW:
                    _character.Forward = 1.0f;
                    break;
                case Key.KeyDown:
                case Key.KeyS:
                    _character.Backward = 1.0f;
                    break;
                case Key.KeyLeft:
                case Key.KeyA:
                    _character.Left = 1.0f;
                    break;
                case Key.KeyRight:
                case Key.KeyD:
                    _character.Right = 1.0f;
                    break;
                case Key.KeySpace:
                    _character.Jump = true;
                    break;
            }
        }

        private void CreateScene()
        {
            var cache = GetSubsystem<ResourceCache>();

            Scene = new Scene(Context);
            Scene.LoadXML("Scenes/Scene.xml");
            Scene.CreateComponent<Octree>();
            Scene.CreateComponent<PhysicsWorld>();
            
            var zone = Scene.CreateComponent<Zone>();
            zone.SetBoundingBox(new BoundingBox(-1000, 1000));
            zone.SetBackgroundBrightness(0.25f);
            zone.AmbientColor = new Color(0.1f, 0.1f, 0.1f, 1);
            var skybox = Scene.CreateComponent<Skybox>();
            skybox.SetModel(cache.GetResource<Model>("Models/Box.mdl"));
            skybox.SetMaterial(cache.GetResource<Material>("Materials/Default-Skybox.xml"));
            zone.ZoneTexture = cache.GetResource<TextureCube>("Textures/ReflectionProbe-0.xml");

            var light = Scene.CreateChild();
            var l = light.CreateComponent<Light>();
            l.LightType = LightType.LightDirectional;
            l.CastShadows = true;
            l.Brightness = 1.0f;
            light.LookAt(light.Position + new Vector3(-1, -1, -1));
            _player = Scene.CreateChild();
            

            //_spawnPoints = Scene.GetChildrenWithTag("SpawnPoint", true).ToList();
            //_player.Position = (_spawnPoints.Count>0)?_spawnPoints[0].WorldPosition:Vector3.Zero;
            _character = _player.CreateComponent<ClassicFpsCharacter>();
            var camera = _character.Camera;
            camera.Fov = 80;
            //AnimatedModel shadowModel = _player.CreateComponent<AnimatedModel>();
            //shadowModel.CastShadows = true;
            //shadowModel.SetModel(cache.GetResource<Model>("Mixamo/Ch15/Ch15_nonPBR/Ch15.mdl"));
            //shadowModel.SetMaterial(0, cache.GetResource<Material>("Materials/Shadow.xml"));
            //shadowModel.SetMaterial(1, cache.GetResource<Material>("Materials/Shadow.xml "));
            ////shadowModel.SetMaterial(cache.GetResource<Material>("Materials/Shadow.xml"));
            //AnimatedModel armsModel = _player.CreateComponent<AnimatedModel>();
            //armsModel.SetModel(cache.GetResource<Model>("Mixamo/Ch15/Ch15Arms/Ch15.mdl"));
            //armsModel.SetMaterial(0, cache.GetResource<Material>("Mixamo/Ch15/Ch15_nonPBR/Ch15_body.xml"));
            //armsModel.SetMaterial(1, cache.GetResource<Material>("Mixamo/Ch15/Ch15_nonPBR/Ch15_body1.xml"));
            //var animationController = _player.CreateComponent<AnimationController>();
            //animationController.Play("Mixamo/Ch15/idle aiming/mixamo.com.ani", 0, true);
            var gun = camera.Node.CreateChild("Gun");
            gun.LoadXML("AdaptiveCombatRifle/Objects/ACRRifle.xml");
            gun.Position = new Vector3(0.104308f, -0.19f, 0.227534f);
            gun.Rotation = new Quaternion(0.5f, 0.5f, -0.5f, -0.5f);
            gun.GetComponent<RigidBody>().IsEnabled = false;

            //foreach (var spawnPoint in _spawnPoints.Skip(1))
            //{
            //    var enemy = Scene.CreateChild();
            //    enemy.LoadXML("Objects/Enemy.xml");
            //    enemy.WorldPosition = spawnPoint.WorldPosition;
            //    enemy.WorldDirection = spawnPoint.WorldDirection;
            //    var model = enemy.GetComponent<AnimatedModel>(true);
            //    var states = model.AnimationStates;
            //    var a = model.Node.GetOrCreateComponent<AnimationController>();
            //    a.Play(states[0].Animation.Name, 0, true);
            //}

            for (int x = -2; x <= 2; ++x)
            {
                for (int z = -2; z <= 2; ++z)
                {
                    var floorTile = Scene.CreateChild();
                    floorTile.LoadXML("CargoBay/Objects/Floor.xml");
                    floorTile.Position = new Vector3(x*8, 0, z*8);
                }
            }

            float spread = 2 * 8;
            for (int i = 0; i < 5; i++)
            {
                var floorTile = Scene.CreateChild();
                floorTile.LoadXML("CargoBay/Objects/LargeContainer.xml");
                floorTile.Position = new Vector3(MathDefs.Random(-spread, spread), 0, MathDefs.Random(-spread, spread));
            }

            for (int i = 0; i < 20; i++)
            {
                var character = Scene.CreateChild();
                string animation = null;
                switch (MathDefs.Random(0,3))
                {
                    case 0:
                        character.LoadXML("Mixamo/XBot/Models/XBot.xml");
                        break;
                    case 1:
                        character.LoadXML("Mixamo/Swat/Models/Swat.xml");
                        animation = "Mixamo/YBot/Animations/MaleLocomotionPack/mixamo.com.ani";
                        break;
                    default:
                        character.LoadXML("Mixamo/YBot/Models/YBot.xml");
                        animation = "Mixamo/YBot/Animations/MaleLocomotionPack/mixamo.com.ani";
                        break;
                }
                character.Position = new Vector3(MathDefs.Random(-spread, spread), 0, MathDefs.Random(-spread, spread));
                character.CreateComponent<ClassicFpsCharacter>();
                var a = character.CreateComponent<AnimationController>();
                if (animation != null)
                {
                    a.Play(animation, 0, true, 0);
                }
                
            }
            Viewport = new Viewport(Context, Scene, camera);
            Context.Renderer.SetViewport(0, Viewport);
        }
    }
}