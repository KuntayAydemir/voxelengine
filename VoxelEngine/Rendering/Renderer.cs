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

        public Renderer(Camera camera)
        {
            _camera = camera;
            _shader = new Shader(ShaderSources.VertexShader, ShaderSources.FragmentShader);
        }

        public void Render(GameWorld world)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _shader.Use();

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = _camera.GetViewMatrix();
            Matrix4 projection = _camera.GetProjectionMatrix();

            _shader.SetMatrix4("model", model);
            _shader.SetMatrix4("view", view);
            _shader.SetMatrix4("projection", projection);

            world.Render(_shader);

            // Basit debug metni (chunk sayısı, FPS) için gelecekte bir UI katmanı ekleyeceğiz
        }
    }
}