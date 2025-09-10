using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace VoxelEngine.Rendering
{
    public class Shader
    {
        private readonly int _handle;

        public Shader(string vertexSource, string fragmentSource)
        {
            _handle = GL.CreateProgram();

            int vertexShader = CompileShader(vertexSource, ShaderType.VertexShader);
            int fragmentShader = CompileShader(fragmentSource, ShaderType.FragmentShader);

            GL.AttachShader(_handle, vertexShader);
            GL.AttachShader(_handle, fragmentShader);

            GL.LinkProgram(_handle);

            GL.DetachShader(_handle, vertexShader);
            GL.DetachShader(_handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        private static int CompileShader(string source, ShaderType type)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                System.Console.WriteLine($"Shader compile error: {infoLog}");
            }

            return shader;
        }

        public void Use()
        {
            GL.UseProgram(_handle);
        }

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            int location = GL.GetUniformLocation(_handle, name);
            GL.UniformMatrix4(location, false, ref matrix);
        }
        
        public void SetVector3(string name, Vector3 value)
        {
            int location = GL.GetUniformLocation(_handle, name);
            GL.Uniform3(location, value.X, value.Y, value.Z);
        }
        
        public void SetFloat(string name, float value)
        {
            int location = GL.GetUniformLocation(_handle, name);
            GL.Uniform1(location, value);
        }
        
        public void Dispose()
        {
            GL.DeleteProgram(_handle);
        }
    }
}
