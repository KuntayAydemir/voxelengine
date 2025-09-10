using OpenTK.Graphics.OpenGL4;
using System;

namespace VoxelEngine.Rendering
{
    // Full screen quad for displaying the path traced result
    public class FullscreenQuad
    {
        private int VAO, VBO, EBO;
        private int shaderProgram;
        
        public FullscreenQuad()
        {
            SetupGeometry();
            SetupShader();
        }
        
        private void SetupGeometry()
        {
            float[] vertices = {
                -1.0f, -1.0f, 0.0f, 0.0f, 0.0f,
                 1.0f, -1.0f, 0.0f, 1.0f, 0.0f,
                 1.0f,  1.0f, 0.0f, 1.0f, 1.0f,
                -1.0f,  1.0f, 0.0f, 0.0f, 1.0f
            };
            
            uint[] indices = { 0, 1, 2, 0, 2, 3 };
            
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            EBO = GL.GenBuffer();
            
            GL.BindVertexArray(VAO);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            GL.BindVertexArray(0);
        }
        
        private void SetupShader()
        {
            string vertexSource = @"
            #version 330 core
            layout (location = 0) in vec3 position;
            layout (location = 1) in vec2 texCoord;
            
            out vec2 TexCoord;
            
            void main()
            {
                gl_Position = vec4(position, 1.0);
                TexCoord = texCoord;
            }";
            
            string fragmentSource = @"
            #version 330 core
            in vec2 TexCoord;
            out vec4 FragColor;
            
            uniform sampler2D screenTexture;
            uniform float exposure;
            uniform float gamma;
            
            void main()
            {
                vec3 color = texture(screenTexture, TexCoord).rgb;
                
                // Optional post-processing
                // color = vec3(1.0) - exp(-color * exposure); // Tone mapping
                // color = pow(color, vec3(1.0 / gamma)); // Gamma correction
                
                FragColor = vec4(color, 1.0);
            }";
            
            int vertexShader = CompileShader(vertexSource, ShaderType.VertexShader);
            int fragmentShader = CompileShader(fragmentSource, ShaderType.FragmentShader);
            
            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }
        
        private int CompileShader(string source, ShaderType type)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            return shader;
        }
        
        public void Render(int texture, float exposure = 1.0f, float gamma = 2.2f)
        {
            GL.UseProgram(shaderProgram);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "screenTexture"), 0);
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "exposure"), exposure);
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "gamma"), gamma);
            
            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
        
        public void Dispose()
        {
            GL.DeleteVertexArray(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteProgram(shaderProgram);
        }
    }
}
