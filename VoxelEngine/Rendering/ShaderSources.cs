namespace VoxelEngine.Rendering;

public static class ShaderSources
{
    public static readonly string VertexShader = @"
#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in float aBlockType;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec2 texCoord;
out vec3 worldPos;
out vec3 normal;
out float blockType;
out float fogFactor;

void main()
{
    worldPos = vec3(model * vec4(aPosition, 1.0));
    texCoord = aTexCoord;
    normal = normalize(mat3(model) * aNormal);
    blockType = aBlockType;
    
    gl_Position = projection * view * vec4(worldPos, 1.0);
    
    // Calculate fog factor
    float distance = length((view * vec4(worldPos, 1.0)).xyz);
    fogFactor = exp(-distance * 0.01);
    fogFactor = clamp(fogFactor, 0.0, 1.0);
}";

    public static readonly string FragmentShader = @"
#version 330 core

in vec2 texCoord;
in vec3 worldPos;
in vec3 normal;
in float blockType;
in float fogFactor;

out vec4 FragColor;

uniform vec3 lightDir = vec3(0.5, -1.0, 0.3);
uniform vec3 lightColor = vec3(1.0, 1.0, 1.0);
uniform vec3 ambientLight = vec3(0.3, 0.3, 0.3);
uniform vec3 fogColor = vec3(0.5, 0.7, 1.0); // Sky blue fog

void main()
{
    // Simple solid colors based on block type - no texture bleeding!
    vec3 color;
    
    int blockTypeInt = int(blockType);
    if (blockTypeInt == 1) { // Stone
        color = vec3(0.5, 0.5, 0.5);  // Gray
    } else if (blockTypeInt == 2) { // Dirt
        color = vec3(0.6, 0.4, 0.2);  // Brown
    } else if (blockTypeInt == 3) { // Grass
        color = vec3(0.2, 0.8, 0.2);  // Pure green
    } else if (blockTypeInt == 4) { // Wood
        color = vec3(0.8, 0.6, 0.4);  // Light brown
    } else if (blockTypeInt == 5) { // Leaves
        color = vec3(0.1, 0.6, 0.1);  // Dark green
    } else { // Default/Air
        color = vec3(0.7, 0.7, 0.7);  // Light gray
    }
    
    // Pixelated lighting - round to steps for retro look
    float diffuse = max(dot(normal, -normalize(lightDir)), 0.0);
    diffuse = floor(diffuse * 4.0) / 4.0;  // Quantize lighting for pixelated look
    
    // Apply quantized lighting to solid color
    vec3 finalColor = color * (ambientLight + lightColor * diffuse);
    
    // Apply fog for distance
    finalColor = mix(fogColor, finalColor, fogFactor);
    
    FragColor = vec4(finalColor, 1.0);
}";
}
