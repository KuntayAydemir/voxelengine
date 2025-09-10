using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace VoxelEngine.Rendering
{
    // Path Tracing için Ray yapısı
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Ray
    {
        public Vector3 Origin;
        public float _padding1;
        public Vector3 Direction;
        public float _padding2;
        
        public Ray(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = Vector3.Normalize(direction);
            _padding1 = 0;
            _padding2 = 0;
        }
    }

    // Material properties for path tracing
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct VoxelMaterial
    {
        public Vector3 Albedo;
        public float Roughness;
        public Vector3 Emission;
        public float Metallic;
        public float Transparency;
        public Vector3 _padding;
        
        public VoxelMaterial(Vector3 albedo, float roughness = 1.0f, Vector3 emission = default, float metallic = 0.0f, float transparency = 0.0f)
        {
            Albedo = albedo;
            Roughness = roughness;
            Emission = emission;
            Metallic = metallic;
            Transparency = transparency;
            _padding = Vector3.Zero;
        }
    }

    // Path tracing renderer
    public class PathTracingRenderer
    {
        private int computeShader;
        private int accumulationTexture;
        private int outputTexture;
        private int voxelSSBO; // Shader Storage Buffer Object
        private int materialSSBO;
        
        private int sampleCount = 0;
        private const int MAX_SAMPLES = 1000;
        private const int MAX_BOUNCES = 8;
        
        private Sky _sky;
        private Random random;
        
        public int Width { get; private set; }
        public int Height { get; private set; }
        
        // Material definitions
        private VoxelMaterial[] materials = new VoxelMaterial[]
        {
            new VoxelMaterial(new Vector3(0.0f), 1.0f, Vector3.Zero, 0.0f), // Air
            new VoxelMaterial(new Vector3(0.6f, 0.6f, 0.6f), 0.8f, Vector3.Zero, 0.0f), // Stone
            new VoxelMaterial(new Vector3(0.6f, 0.4f, 0.2f), 0.9f, Vector3.Zero, 0.0f), // Dirt
            new VoxelMaterial(new Vector3(0.2f, 0.8f, 0.2f), 0.9f, Vector3.Zero, 0.0f), // Grass
            new VoxelMaterial(new Vector3(0.8f, 0.6f, 0.4f), 0.7f, Vector3.Zero, 0.0f), // Wood
            new VoxelMaterial(new Vector3(0.1f, 0.6f, 0.1f), 0.9f, Vector3.Zero, 0.0f), // Leaves
            new VoxelMaterial(new Vector3(1.0f, 1.0f, 0.8f), 1.0f, new Vector3(5.0f, 5.0f, 4.0f), 0.0f), // Light source
            new VoxelMaterial(new Vector3(0.9f, 0.9f, 0.8f), 0.1f, Vector3.Zero, 1.0f), // Metal
        };
        
        public PathTracingRenderer(int width, int height, Sky sky)
        {
            Width = width;
            Height = height;
            _sky = sky;
            random = new Random();
            
            InitializeShaders();
            InitializeTextures();
            InitializeBuffers();
            UpdateMaterials(materials);
        }
        
        private void InitializeShaders()
        {
            string computeSource = @"
            #version 430
            layout(local_size_x = 8, local_size_y = 8) in;
            
            layout(binding = 0, rgba32f) uniform image2D accumulationImage;
            layout(binding = 1, rgba8) uniform image2D outputImage;
            
            // Voxel data buffer
            layout(std430, binding = 2) buffer VoxelBuffer
            {{
                uint voxelData[];
            }};
            
            // Material buffer
            layout(std430, binding = 3) buffer MaterialBuffer
            {{
                vec4 materials[]; // albedo.rgb + roughness, emission.rgb + metallic
            }};
            
            // Uniforms
            uniform mat4 invViewMatrix;
            uniform mat4 invProjMatrix;
            uniform vec3 cameraPos;
            uniform vec3 sunDirection;
            uniform vec3 sunColor;
            uniform float sunIntensity;
            uniform float sunAngularSize;
            uniform vec3 atmosphereColor;
            uniform float atmosphereDensity;
            uniform int frameCount;
            uniform int maxBounces;
            uniform ivec3 worldSize;
            
            // Random number generator
            uint rng_state;
            
            uint wang_hash(uint seed)
            {{
                seed = (seed ^ 61u) ^ (seed >> 16u);
                seed *= 9u;
                seed = seed ^ (seed >> 4u);
                seed *= 0x27d4eb2du;
                seed = seed ^ (seed >> 15u);
                return seed;
            }}
            
            float random_float()
            {{
                rng_state = wang_hash(rng_state);
                return float(rng_state) / 4294967296.0;
            }}
            
            vec3 random_hemisphere(vec3 normal)
            {{
                float u1 = random_float();
                float u2 = random_float();
                
                float theta = acos(sqrt(u1));
                float phi = 2.0 * 3.14159 * u2;
                
                vec3 w = normal;
                vec3 u = normalize(cross(w, vec3(0.0, 1.0, 0.0)));
                if (length(u) < 0.1) u = normalize(cross(w, vec3(1.0, 0.0, 0.0)));
                vec3 v = cross(w, u);
                
                return sin(theta) * cos(phi) * u + sin(theta) * sin(phi) * v + cos(theta) * w;
            }}
            
            // Voxel ray intersection using DDA
            bool rayVoxelIntersect(vec3 rayOrigin, vec3 rayDirection, out vec3 hitPoint, out vec3 hitNormal, out int materialId)
            {{
                vec3 pos = rayOrigin;
                ivec3 voxelPos = ivec3(floor(pos));
                vec3 deltaDist = abs(1.0 / rayDirection);
                ivec3 step = ivec3(sign(rayDirection));
                vec3 sideDist = (sign(rayDirection) * (vec3(voxelPos) - pos) + (sign(rayDirection) * 0.5) + 0.5) * deltaDist;
                
                bvec3 mask = bvec3(false);
                float maxDist = 200.0;
                
                for (int i = 0; i < 400 && distance(rayOrigin, pos) < maxDist; i++)
                {{
                    // Check bounds
                    if (voxelPos.x >= 0 && voxelPos.y >= 0 && voxelPos.z >= 0 && 
                        voxelPos.x < worldSize.x && voxelPos.y < worldSize.y && voxelPos.z < worldSize.z)
                    {{
                        int index = voxelPos.x + voxelPos.z * worldSize.x + voxelPos.y * worldSize.x * worldSize.z;
                        if (index >= 0 && index < voxelData.length())
                        {{
                            uint voxel = voxelData[index];
                            
                            if (voxel != 0u) // Hit solid voxel
                            {{
                                hitPoint = pos;
                                hitNormal = vec3(0.0);
                                if (mask.x) hitNormal.x = -step.x;
                                else if (mask.y) hitNormal.y = -step.y;
                                else if (mask.z) hitNormal.z = -step.z;
                                
                                materialId = int(voxel & 0xFFu);
                                return true;
                            }}
                        }}
                    }}
                    
                    // Step to next voxel
                    mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));
                    sideDist += vec3(mask) * deltaDist;
                    voxelPos += ivec3(vec3(mask)) * step;
                    pos = rayOrigin + rayDirection * min(sideDist.x, min(sideDist.y, sideDist.z));
                }}
                
                return false;
            }}
            
            // Sky sampling
            vec3 sampleSky(vec3 direction)
            {{
                float sunDot = dot(direction, sunDirection);
                
                // Sun disk
                if (sunDot > 0.99)
                {{
                    return sunColor * sunIntensity * 2.0;
                }}
                
                // Simple atmospheric scattering
                float elevation = max(0.0, direction.y);
                vec3 skyColor = mix(
                    vec3(1.0, 0.8, 0.6), // Horizon
                    vec3(0.5, 0.7, 1.0), // Zenith
                    elevation
                );
                
                // Sun halo
                float sunHalo = pow(max(0.0, sunDot), 16.0);
                skyColor += sunColor * sunHalo * 0.5;
                
                return skyColor * atmosphereDensity;
            }}
            
            // Path tracing main function
            vec3 pathTrace(vec3 rayOrigin, vec3 rayDirection)
            {{
                vec3 color = vec3(0.0);
                vec3 throughput = vec3(1.0);
                
                for (int bounce = 0; bounce < 8; bounce++)
                {{
                    vec3 hitPoint, hitNormal;
                    int materialId;
                    
                    if (rayVoxelIntersect(rayOrigin, rayDirection, hitPoint, hitNormal, materialId))
                    {{
                        if (materialId >= materials.length() / 2) materialId = 1; // Fallback
                        
                        // Get material properties
                        vec4 albedoRough = materials[materialId * 2];
                        vec4 emissionMetal = materials[materialId * 2 + 1];
                        
                        vec3 albedo = albedoRough.rgb;
                        float roughness = albedoRough.a;
                        vec3 emission = emissionMetal.rgb;
                        float metallic = emissionMetal.a;
                        
                        // Add emission
                        color += throughput * emission;
                        
                        // Russian roulette termination
                        if (bounce > 2)
                        {{
                            float continueProbability = max(throughput.r, max(throughput.g, throughput.b));
                            if (random_float() > continueProbability) break;
                            throughput /= continueProbability;
                        }}
                        
                        // Sample new direction
                        vec3 newDirection;
                        if (metallic > 0.5) // Metallic reflection
                        {{
                            newDirection = reflect(rayDirection, hitNormal);
                            newDirection = normalize(mix(newDirection, random_hemisphere(hitNormal), roughness));
                        }}
                        else // Diffuse reflection
                        {{
                            newDirection = random_hemisphere(hitNormal);
                        }}
                        
                        // Update throughput
                        float NdotL = max(0.0, dot(hitNormal, newDirection));
                        throughput *= albedo * NdotL;
                        
                        // Offset to avoid self-intersection
                        rayOrigin = hitPoint + hitNormal * 0.001;
                        rayDirection = newDirection;
                    }}
                    else
                    {{
                        // Hit sky
                        color += throughput * sampleSky(rayDirection);
                        break;
                    }}
                }}
                
                return color;
            }}
            
            void main()
            {{
                ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
                ivec2 imageSize = imageSize(accumulationImage);
                
                if (coords.x >= imageSize.x || coords.y >= imageSize.y)
                    return;
                
                // Initialize RNG
                rng_state = uint(coords.x + coords.y * imageSize.x) * uint(frameCount + 1);
                
                // Generate camera ray with anti-aliasing
                vec2 uv = (vec2(coords) + vec2(random_float(), random_float())) / vec2(imageSize);
                vec2 ndc = uv * 2.0 - 1.0;
                
                vec4 nearPlane = invProjMatrix * vec4(ndc, -1.0, 1.0);
                nearPlane /= nearPlane.w;
                
                vec4 worldNear = invViewMatrix * nearPlane;
                vec3 rayDirection = normalize(worldNear.xyz - cameraPos);
                
                // Path trace
                vec3 color = pathTrace(cameraPos, rayDirection);
                
                // Accumulate samples
                vec4 prevColor = imageLoad(accumulationImage, coords);
                vec4 newColor = vec4(color, 1.0);
                
                float weight = 1.0 / float(frameCount + 1);
                vec4 accumulated = mix(prevColor, newColor, weight);
                
                imageStore(accumulationImage, coords, accumulated);
                
                // Tone mapping for output
                vec3 toneMapped = accumulated.rgb / (accumulated.rgb + vec3(1.0));
                toneMapped = pow(toneMapped, vec3(1.0 / 2.2));
                
                imageStore(outputImage, coords, vec4(toneMapped, 1.0));
            }}";
            
            computeShader = CompileComputeShader(computeSource);
        }
        
        private void InitializeTextures()
        {
            // Accumulation texture (high precision)
            accumulationTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, accumulationTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, 
                         Width, Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            
            // Output texture (display)
            outputTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, outputTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                         Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }
        
        private void InitializeBuffers()
        {
            voxelSSBO = GL.GenBuffer();
            materialSSBO = GL.GenBuffer();
        }
        
        public void UpdateVoxelData(VoxelEngine.World.GameWorld world)
        {
            const int WORLD_SIZE = 128; // Küçük test boyutu
            uint[] voxelData = new uint[WORLD_SIZE * WORLD_SIZE * WORLD_SIZE];
            
            // Convert chunk data to voxel array
            int index = 0;
            for (int y = 0; y < WORLD_SIZE; y++)
            {
                for (int z = 0; z < WORLD_SIZE; z++)
                {
                    for (int x = 0; x < WORLD_SIZE; x++)
                    {
                        var blockType = world.GetBlock(new Vector3(x, y, z));
                        voxelData[index++] = (uint)blockType;
                    }
                }
            }
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, voxelSSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, voxelData.Length * sizeof(uint), 
                         voxelData, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, voxelSSBO);
        }
        
        private void UpdateMaterials(VoxelMaterial[] materials)
        {
            float[] materialData = new float[materials.Length * 8]; // 2 vec4 per material
            
            for (int i = 0; i < materials.Length; i++)
            {
                int baseIndex = i * 8;
                var mat = materials[i];
                
                // First vec4: albedo + roughness
                materialData[baseIndex + 0] = mat.Albedo.X;
                materialData[baseIndex + 1] = mat.Albedo.Y;
                materialData[baseIndex + 2] = mat.Albedo.Z;
                materialData[baseIndex + 3] = mat.Roughness;
                
                // Second vec4: emission + metallic
                materialData[baseIndex + 4] = mat.Emission.X;
                materialData[baseIndex + 5] = mat.Emission.Y;
                materialData[baseIndex + 6] = mat.Emission.Z;
                materialData[baseIndex + 7] = mat.Metallic;
            }
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, materialSSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, materialData.Length * sizeof(float),
                         materialData, BufferUsageHint.StaticDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, materialSSBO);
        }
        
        public void Render(Matrix4 viewMatrix, Matrix4 projectionMatrix, Vector3 cameraPosition)
        {
            GL.UseProgram(computeShader);
            
            // Bind textures
            GL.BindImageTexture(0, accumulationTexture, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            GL.BindImageTexture(1, outputTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba8);
            
            // Set uniforms
            Matrix4 invView = viewMatrix.Inverted();
            Matrix4 invProj = projectionMatrix.Inverted();
            
            GL.UniformMatrix4(GL.GetUniformLocation(computeShader, "invViewMatrix"), false, ref invView);
            GL.UniformMatrix4(GL.GetUniformLocation(computeShader, "invProjMatrix"), false, ref invProj);
            GL.Uniform3(GL.GetUniformLocation(computeShader, "cameraPos"), cameraPosition);
            
            GL.Uniform3(GL.GetUniformLocation(computeShader, "sunDirection"), _sky.SunDirection);
            GL.Uniform3(GL.GetUniformLocation(computeShader, "sunColor"), _sky.SunColor);
            GL.Uniform1(GL.GetUniformLocation(computeShader, "sunIntensity"), _sky.SunIntensity);
            GL.Uniform1(GL.GetUniformLocation(computeShader, "sunAngularSize"), 0.01f);
            GL.Uniform3(GL.GetUniformLocation(computeShader, "atmosphereColor"), _sky.SkyColor);
            GL.Uniform1(GL.GetUniformLocation(computeShader, "atmosphereDensity"), 1.0f);
            
            GL.Uniform1(GL.GetUniformLocation(computeShader, "frameCount"), sampleCount);
            GL.Uniform1(GL.GetUniformLocation(computeShader, "maxBounces"), MAX_BOUNCES);
            GL.Uniform3(GL.GetUniformLocation(computeShader, "worldSize"), new Vector3(128, 128, 128));
            
            // Dispatch compute shader
            int workGroupsX = (Width + 7) / 8;
            int workGroupsY = (Height + 7) / 8;
            GL.DispatchCompute(workGroupsX, workGroupsY, 1);
            
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            
            sampleCount++;
        }
        
        public void ResetAccumulation()
        {
            sampleCount = 0;
            
            // Clear accumulation texture
            GL.BindTexture(TextureTarget.Texture2D, accumulationTexture);
            float[] clearData = new float[Width * Height * 4];
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f,
                         Width, Height, 0, PixelFormat.Rgba, PixelType.Float, clearData);
        }
        
        public int GetOutputTexture() => outputTexture;
        
        private int CompileComputeShader(string source)
        {
            int shader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                string error = GL.GetShaderInfoLog(shader);
                throw new Exception($"Compute shader compilation failed: {error}");
            }
            
            int program = GL.CreateProgram();
            GL.AttachShader(program, shader);
            GL.LinkProgram(program);
            
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                string error = GL.GetProgramInfoLog(program);
                throw new Exception($"Compute shader linking failed: {error}");
            }
            
            GL.DeleteShader(shader);
            return program;
        }
        
        public void Dispose()
        {
            GL.DeleteProgram(computeShader);
            GL.DeleteTexture(accumulationTexture);
            GL.DeleteTexture(outputTexture);
            GL.DeleteBuffer(voxelSSBO);
            GL.DeleteBuffer(materialSSBO);
        }
    }
}
