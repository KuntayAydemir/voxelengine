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

        private double _fpsTimer = 0.0;
        private int _frameCounter = 0;
        private bool _wireframeMode = false;

        public GameWindow() : base(
            new GameWindowSettings()
            {
                UpdateFrequency = 60.0
            },
            new NativeWindowSettings()
            {
                ClientSize = (1920, 1080),
                Title = "Voxel Engine",
                Flags = ContextFlags.ForwardCompatible,
                Vsync = VSyncMode.On // VSync aktif
            })
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            _camera = new Camera();
            _world = new GameWorld();
            _player = new PlayerPhysics(_world);
            _renderer = new Renderer(_camera);
            
            // Kamera ve oyuncu pozisyonlarını senkronize et
            _camera.Position = _player.Position;

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            HandleInput(e);

            _player.Update((float)e.Time);
            _world.Update((float)e.Time, _player.Position);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _renderer.Render(_world);

            // FPS hesapla ve başlığa yaz
            _frameCounter++;
            _fpsTimer += e.Time;
            if (_fpsTimer >= 1.0)
            {
                int fps = _frameCounter;
                _frameCounter = 0;
                _fpsTimer = 0.0;
                string wireframeText = _wireframeMode ? " | Wireframe: ON" : "";
                Title = $"Voxel Engine | FPS: {fps} | Chunks: {_world.Chunks.Count} | Mode: {_player.Mode} | Pos: {_player.Position.X:F1}, {_player.Position.Y:F1}, {_player.Position.Z:F1}{wireframeText}";
            }

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
        }

        private void HandleInput(FrameEventArgs e)
        {
            var keyboardState = KeyboardState;
            var mouseState = MouseState;

            if (keyboardState.IsKeyDown(Keys.Escape))
                Close();

            float speed = 15.0f; // deltaTime PlayerPhysics'te uygulanacak

            Vector3 moveDir = Vector3.Zero;
            if (keyboardState.IsKeyDown(Keys.W))
                moveDir += _camera.Front;
            if (keyboardState.IsKeyDown(Keys.S))
                moveDir -= _camera.Front;
            if (keyboardState.IsKeyDown(Keys.A))
                moveDir -= _camera.Right;
            if (keyboardState.IsKeyDown(Keys.D))
                moveDir += _camera.Right;
            moveDir.Y = 0; // yatay düzlemde
            if (moveDir.LengthSquared > 0)
                moveDir = Vector3.Normalize(moveDir) * speed;

            // God mod yükselme/alçalma
            if (_player.Mode == PlayerMode.God)
            {
                if (keyboardState.IsKeyDown(Keys.Space)) moveDir.Y += speed;
                if (keyboardState.IsKeyDown(Keys.LeftShift)) moveDir.Y -= speed;
            }
            else
            {
                // Normal modda zıplama
                if (keyboardState.IsKeyPressed(Keys.Space)) _player.Jump();
            }

            _player.ApplyMovement(moveDir, (float)e.Time);
            _camera.Position = _player.Position;

            // Mod toggle
            if (keyboardState.IsKeyPressed(Keys.F10))
            {
                _player.ToggleMode();
            }
            
            // Wireframe toggle
            if (keyboardState.IsKeyPressed(Keys.F1))
            {
                _wireframeMode = !_wireframeMode;
                if (_wireframeMode)
                {
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    GL.Disable(EnableCap.CullFace);
                }
                else
                {
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    GL.Enable(EnableCap.CullFace);
                }
            }

            // Basit mouse-look
            const float sensitivity = 0.1f;
            var delta = mouseState.Delta; // px/frame

            if (CursorState == CursorState.Grabbed)
            {
                _camera.YawPitch(delta.X * sensitivity, -delta.Y * sensitivity);
            }
        }
    }
}
