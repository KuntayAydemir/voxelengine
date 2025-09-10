using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace VoxelEngine.Rendering
{
    public class Sky
    {
        private int _vao, _vbo;
        private Shader _skyShader;
        
        // Sun properties
        public Vector3 SunDirection { get; set; } = Vector3.Normalize(new Vector3(0.5f, -1.0f, 0.3f));
        public Vector3 SunColor { get; set; } = new Vector3(1.0f, 0.9f, 0.7f);
        public float SunIntensity { get; set; } = 1.0f;
        
        // Sky properties
        public Vector3 SkyColor { get; set; } = new Vector3(0.5f, 0.7f, 1.0f);
        public Vector3 HorizonColor { get; set; } = new Vector3(0.8f, 0.9f, 1.0f);
        
        // Time of day (0.0 = midnight, 0.5 = noon, 1.0 = midnight)
        public float TimeOfDay { get; set; } = 0.45f; // Öğlene yakın - güneş görünür
        
        public Sky()
        {
            InitializeSkybox();
            _skyShader = new Shader(SkyShaderSources.VertexShader, SkyShaderSources.FragmentShader);
        }
        
        private void InitializeSkybox()
        {
            // Skybox cube vertices
            float[] skyboxVertices = {
                // positions          
                -1.0f,  1.0f, -1.0f,
                -1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,

                -1.0f, -1.0f,  1.0f,
                -1.0f, -1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f,  1.0f,
                -1.0f, -1.0f,  1.0f,

                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,

                -1.0f, -1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f,
                -1.0f, -1.0f,  1.0f,

                -1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,
                -1.0f,  1.0f, -1.0f,

                -1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f
            };

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, skyboxVertices.Length * sizeof(float), skyboxVertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }
        
        public void UpdateTimeOfDay(float deltaTime)
        {
            // Simple day/night cycle - yaklaşık 10 dakikalık gün/gece döngüsü
            TimeOfDay += deltaTime * 0.001f; // Yavaş günеş hareketi
            if (TimeOfDay > 1.0f) TimeOfDay -= 1.0f;
            
            // Sun direction based on time of day - güneş yerin üstünde kalacak şekilde
            float sunAngle = TimeOfDay * MathF.PI * 2.0f; // 0-2π
            float sunHeight = MathF.Cos(sunAngle); // -1 to 1
            
            // Güneşi yerin üstünde tut (minimum 0.1 yükseklik)
            sunHeight = MathHelper.Clamp(sunHeight, -0.8f, 1.0f);
            
            SunDirection = Vector3.Normalize(new Vector3(
                MathF.Sin(sunAngle) * 0.7f, 
                -sunHeight, // Negatif çünkü light direction (güneşe doğru)
                0.2f
            ));
            
            // Sun intensity - gece minimum 0.05, gündüz maksimum 1.0
            float rawIntensity = MathHelper.Clamp((sunHeight + 0.2f) / 1.2f, 0.0f, 1.0f);
            SunIntensity = MathHelper.Clamp(rawIntensity, 0.05f, 1.0f);
        }
        
        public void Render(Matrix4 view, Matrix4 projection)
        {
            // Skybox depth test devre dışı ve en son render et
            GL.DepthFunc(DepthFunction.Lequal);
            
            _skyShader.Use();
            
            // Remove translation from view matrix  
            var viewNoTranslation = new Matrix4(
                new Vector4(view.Row0.Xyz, 0),
                new Vector4(view.Row1.Xyz, 0), 
                new Vector4(view.Row2.Xyz, 0),
                new Vector4(0, 0, 0, 1)
            );
            
            _skyShader.SetMatrix4("view", viewNoTranslation);
            _skyShader.SetMatrix4("projection", projection);
            _skyShader.SetVector3("sunDirection", SunDirection);
            _skyShader.SetVector3("sunColor", SunColor);
            _skyShader.SetFloat("sunIntensity", SunIntensity);
            _skyShader.SetVector3("skyColor", SkyColor);
            _skyShader.SetFloat("timeOfDay", TimeOfDay);

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            
            GL.DepthFunc(DepthFunction.Less);
        }
        
        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            _skyShader?.Dispose();
        }
    }
    
    public static class SkyShaderSources
    {
        public static readonly string VertexShader = @"
#version 330 core
layout (location = 0) in vec3 aPos;

out vec3 TexCoords;

uniform mat4 projection;
uniform mat4 view;

void main()
{
    TexCoords = aPos;
    vec4 pos = projection * view * vec4(aPos, 1.0);
    gl_Position = pos.xyww; // Trick to keep skybox at far plane
}";

        public static readonly string FragmentShader = @"
#version 330 core
out vec4 FragColor;

in vec3 TexCoords;

uniform vec3 sunDirection;
uniform vec3 sunColor;
uniform float sunIntensity;
uniform vec3 skyColor;
uniform float timeOfDay;

void main()
{
    vec3 direction = normalize(TexCoords);
    
    // Sky gradient (simple atmospheric scattering approximation)
    float height = direction.y;
    vec3 horizonColor = vec3(0.8, 0.9, 1.0);
    vec3 zenithColor = skyColor;
    
    // Day/night transition
    float dayFactor = clamp(sunIntensity * 2.0, 0.0, 1.0);
    vec3 nightSky = vec3(0.01, 0.01, 0.05);
    zenithColor = mix(nightSky, zenithColor, dayFactor);
    horizonColor = mix(nightSky * 2.0, horizonColor, dayFactor);
    
    // Interpolate based on height
    float t = clamp((height + 1.0) * 0.5, 0.0, 1.0);
    t = smoothstep(0.0, 1.0, t); // Smooth transition
    
    vec3 skyGradient = mix(horizonColor, zenithColor, t);
    
    // Sun disk - daha büyük ve görünür
    float sunDot = dot(direction, -sunDirection);
    float sunSize = 0.99; // Daha büyük güneş
    
    if (sunDot > sunSize && sunIntensity > 0.05)
    {
        float sunAlpha = smoothstep(sunSize, 1.0, sunDot);
        vec3 finalSunColor = sunColor * (2.0 + sunIntensity * 3.0);
        skyGradient = mix(skyGradient, finalSunColor, sunAlpha);
    }
    
    // Sun glow effect - çevresinde parlama
    float glowDot = dot(direction, -sunDirection);
    if (glowDot > 0.95)
    {
        float glowFactor = smoothstep(0.95, 1.0, glowDot);
        skyGradient += sunColor * glowFactor * sunIntensity * 0.5;
    }
    
    FragColor = vec4(skyGradient, 1.0);
}";
    }
}
