using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using VoxelEngine.Rendering;
using VoxelEngine.World;
using VoxelEngine.Physics;


namespace VoxelEngine.Core;

public class GameWindow : OpenTK.Windowing.Desktop.GameWindow
{
    private Camera _camera = null!;
    private double _time;
    private bool _firstMove = true;
    private Vector2 _lastPos;
    private Shader _blockShader = null!;
    private GameWorld _world = null!;
    private PlayerPhysics _physics = null!;
    private Vector3 _playerPosition;
    private readonly int _seed;
    
    // FPS Counter
    private int _frameCount = 0;
    private double _fpsTimer = 0.0;
    private double _currentFps = 0.0;
    
    // Debug modes
    private bool _wireframe = false; // Start in solid mode
    private bool _wireframeKeyPressed = false;

    public GameWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, int seed)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _seed = seed;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        GL.ClearColor(0.5f, 0.7f, 1.0f, 1.0f); // Sky blue background
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        
        // Debug: Start in solid mode
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        
        Console.WriteLine($"OpenGL Version: {GL.GetString(StringName.Version)}");
        Console.WriteLine($"OpenGL Renderer: {GL.GetString(StringName.Renderer)}");
        
        _playerPosition = new Vector3(8, 100, 8); // Start in middle of first chunk, very high up to see terrain
        _camera = new Camera(_playerPosition + new Vector3(0, 1.6f, 0), Size.X / (float)Size.Y);
        
        // Initialize shader
        _blockShader = new Shader(ShaderSources.VertexShader, ShaderSources.FragmentShader);
        
        // Initialize world
        _world = new GameWorld(_seed);
        
        // Initialize physics - FIXED: removed pointer syntax
        _physics = new PlayerPhysics(_world);
        
        CursorState = CursorState.Grabbed;
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        
        _time += 4.0 * e.Time;
        
        // Calculate FPS
        _frameCount++;
        _fpsTimer += e.Time;
        if (_fpsTimer >= 1.0)
        {
            _currentFps = _frameCount / _fpsTimer;
            Title = $"Voxel Engine - FPS: {_currentFps:F1} | Pos: ({_playerPosition.X:F1}, {_playerPosition.Y:F1}, {_playerPosition.Z:F1})";
            _frameCount = 0;
            _fpsTimer = 0.0;
        }
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Update world first
        _world.Update(_playerPosition);
        
        // Check for OpenGL errors
        var error = GL.GetError();
        if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
        {
            Console.WriteLine($"OpenGL Error: {error}");
        }
        
        // Render world
        _world.Render(_blockShader, _camera.GetViewMatrix(), _camera.GetProjectionMatrix());
        
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        
        if (!IsFocused) return;

        var input = KeyboardState;
        
        if (input.IsKeyDown(Keys.Escape))
        {
            Close();
        }
        
        // Toggle wireframe with F1 key
        if (input.IsKeyDown(Keys.F1) && !_wireframeKeyPressed)
        {
            _wireframe = !_wireframe;
            GL.PolygonMode(MaterialFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);
            Console.WriteLine($"Wireframe mode: {(_wireframe ? "ON" : "OFF")}");
            _wireframeKeyPressed = true;
        }
        else if (!input.IsKeyDown(Keys.F1))
        {
            _wireframeKeyPressed = false;
        }

        // Calculate movement input
        Vector3 inputMovement = Vector3.Zero;
        const float movementSpeed = 5.0f;

        if (input.IsKeyDown(Keys.W))
            inputMovement += _camera.Front;
        if (input.IsKeyDown(Keys.S))
            inputMovement -= _camera.Front;
        if (input.IsKeyDown(Keys.A))
            inputMovement -= _camera.Right;
        if (input.IsKeyDown(Keys.D))
            inputMovement += _camera.Right;

        // Apply horizontal movement through physics
        if (inputMovement.LengthSquared > 0)
        {
            inputMovement = Vector3.Normalize(inputMovement);
            inputMovement.Y = 0; // Remove vertical component for walking
            _physics.ApplyImpulse(inputMovement * movementSpeed);
        }

        {
            // Fly up
            _playerPosition += Vector3.UnitY * movementSpeed * (float)e.Time;
        }
        if (input.IsKeyDown(Keys.LeftShift))
        {
            // Fly down  
            _playerPosition -= Vector3.UnitY * movementSpeed * (float)e.Time;
        }
        
        // Apply horizontal movement directly (no physics for creative mode)
        if (inputMovement.LengthSquared > 0)
        {
            inputMovement = Vector3.Normalize(inputMovement);
            inputMovement.Y = 0;
            _playerPosition += inputMovement * movementSpeed * (float)e.Time;
        }
        
        // Skip physics for creative mode - comment this out
        // _physics.Update(ref _playerPosition, (float)e.Time, jumpRequested);
        
        // Update camera position to follow player
        _camera.Position = _playerPosition + new Vector3(0, 1.6f, 0); // Eye height offset

        // Mouse look
        var mouse = MouseState;
        const float sensitivity = 0.2f;
        
        if (_firstMove)
        {
            _lastPos = new Vector2(mouse.X, mouse.Y);
            _firstMove = false;
        }
        else
        {
            var deltaX = mouse.X - _lastPos.X;
            var deltaY = mouse.Y - _lastPos.Y;
            _lastPos = new Vector2(mouse.X, mouse.Y);
            
            _camera.Yaw += deltaX * sensitivity;
            _camera.Pitch -= deltaY * sensitivity;
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, Size.X, Size.Y);
        _camera.AspectRatio = Size.X / (float)Size.Y;
    }

    protected override void OnUnload()
    {
        // Clean up resources
        _blockShader?.Dispose();
        
        base.OnUnload();
    }
}