using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelEngine.Physics;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Core
{
    public class GameWindow : OpenTK.Windowing.Desktop.GameWindow
    {
        private GameWorld _world = null!;
        private PlayerPhysics _player = null!;
        private Renderer _renderer = null!;
        private Camera _camera = null!;

        public GameWindow() : base(
            GameWindowSettings.Default,
            new NativeWindowSettings()
            {
                ClientSize = (1920, 1080),
                Title = "Voxel Engine",
                Flags = ContextFlags.ForwardCompatible
            })
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            _camera = new Camera();
            _world = new GameWorld();
            _player = new PlayerPhysics(_world);
            _renderer = new Renderer(_camera);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            HandleInput(e);

            _player.Update((float)e.Time);
            _world.Update((float)e.Time);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _renderer.Render(_world);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
        }

        private void HandleInput(FrameEventArgs e)
        {
            var keyboardState = KeyboardState;

            if (keyboardState.IsKeyDown(Keys.Escape))
                Close();

            float speed = 5.0f * (float)e.Time;

            if (keyboardState.IsKeyDown(Keys.W))
                _camera.Position += _camera.Front * speed;
            if (keyboardState.IsKeyDown(Keys.S))
                _camera.Position -= _camera.Front * speed;
            if (keyboardState.IsKeyDown(Keys.A))
                _camera.Position -= _camera.Right * speed;
            if (keyboardState.IsKeyDown(Keys.D))
                _camera.Position += _camera.Right * speed;
            }
        }
    }