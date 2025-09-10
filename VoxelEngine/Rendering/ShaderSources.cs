namespace VoxelEngine.Rendering;

public static class ShaderSources
{
    public static readonly string VertexShader = @"
#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in float aBlockType;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out float blockType;
out vec3 worldPos;

void main()
{
    worldPos = vec3(model * vec4(aPosition, 1.0));
    blockType = aBlockType;
    gl_Position = projection * view * vec4(worldPos, 1.0);
}";

    public static readonly string FragmentShader = @"
#version 330 core

in float blockType;
in vec3 worldPos;

out vec4 FragColor;

uniform vec3 lightDir = vec3(0.5, -1.0, 0.3);
uniform vec3 lightColor = vec3(1.0, 1.0, 1.0);
uniform vec3 ambientLight = vec3(0.3, 0.3, 0.3);

void main()
{
    vec3 color;
    int blockTypeInt = int(blockType);

    if (blockTypeInt == 1) color = vec3(0.5, 0.5, 0.5);  // Stone
    else if (blockTypeInt == 2) color = vec3(0.6, 0.4, 0.2);  // Dirt
    else if (blockTypeInt == 3) color = vec3(0.2, 0.8, 0.2);  // Grass
    else if (blockTypeInt == 4) color = vec3(0.8, 0.6, 0.4);  // Wood
    else if (blockTypeInt == 5) color = vec3(0.1, 0.6, 0.1);  // Leaves
    else color = vec3(0.7, 0.7, 0.7);  // Default

    vec3 normal = normalize(vec3(0.0, 1.0, 0.0));
    float diffuse = max(dot(normal, -normalize(lightDir)), 0.0);
    vec3 finalColor = color * (ambientLight + lightColor * diffuse);

    FragColor = vec4(finalColor, 1.0);
}";
}