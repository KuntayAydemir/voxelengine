using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelEngine.Core;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Rendering
{
    public class Renderer
    {
        private Shader _shader;
        private Camera _camera;
        private Sky _sky;
        private PathTracingRenderer _pathTracer;
        private FullscreenQuad _fullscreenQuad;
        private Vector3 _lastCameraPosition;
        private Vector3 _lastCameraRotation;
        
        public bool PathTracingEnabled { get; set; } = false;

        public Renderer(Camera camera)
        {
            _camera = camera;
            _shader = new Shader(ShaderSources.VertexShader, ShaderSources.FragmentShader);
            _sky = new Sky();
            
            // Initialize path tracing (lower resolution for performance)
            _pathTracer = new PathTracingRenderer(960, 540, _sky); // Half resolution
            _fullscreenQuad = new FullscreenQuad();
        }

        public void Render(GameWorld world, float deltaTime)
        {
            Matrix4 model = Matrix4.Identity;
            Matrix4 view = _camera.GetViewMatrix();
            Matrix4 projection = _camera.GetProjectionMatrix();
            
            // Update sky/sun system
            _sky.UpdateTimeOfDay(deltaTime);
            
            if (PathTracingEnabled)
            {
                // Path tracing mode
                
                // Check if camera moved to reset accumulation
                Vector3 currentPos = _camera.Position;
                Vector3 currentRot = new Vector3(_camera._pitch, _camera._yaw, 0); // Assuming we add these properties
                
                if (Vector3.Distance(currentPos, _lastCameraPosition) > 0.01f ||
                    Vector3.Distance(currentRot, _lastCameraRotation) > 0.01f)
                {
                    _pathTracer.ResetAccumulation();
                    _lastCameraPosition = currentPos;
                    _lastCameraRotation = currentRot;
                }
                
                // Update voxel data periodically
                _pathTracer.UpdateVoxelData(world);
                
                // Render with path tracing
                _pathTracer.Render(view, projection, _camera.Position);
                
                // Display result
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                _fullscreenQuad.Render(_pathTracer.GetOutputTexture());
            }
            else
            {
                // Traditional rasterization mode
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                
                // Render world blocks first
                _shader.Use();
                _shader.SetMatrix4("model", model);
                _shader.SetMatrix4("view", view);
                _shader.SetMatrix4("projection", projection);
                
                // Dynamic lighting güneşin pozisyonuna göre
                _shader.SetVector3("lightDir", _sky.SunDirection);
                _shader.SetVector3("lightColor", _sky.SunColor * _sky.SunIntensity);
                
                // Ambient light güneşin şiddetine göre - daha parlak
                Vector3 ambientLight = Vector3.One * (0.4f + _sky.SunIntensity * 0.4f);
                _shader.SetVector3("ambientLight", ambientLight);

                world.Render(_shader);
                
                // Sky render en son (depth buffer optimization)
                _sky.Render(view, projection);
            }
        }
        
        public void Dispose()
        {
            _shader?.Dispose();
            _sky?.Dispose();
            _pathTracer?.Dispose();
            _fullscreenQuad?.Dispose();
        }
    }
}