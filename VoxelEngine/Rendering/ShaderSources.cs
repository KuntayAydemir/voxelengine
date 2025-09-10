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

out float blockType;
out vec3 worldPos;
out vec3 normal;
out vec2 texCoord;

void main()
{
    worldPos = vec3(model * vec4(aPosition, 1.0));
    blockType = aBlockType;
    normal = mat3(transpose(inverse(model))) * aNormal;
    texCoord = aTexCoord;
    gl_Position = projection * view * vec4(worldPos, 1.0);
}";

    public static readonly string FragmentShader = @"
#version 330 core

in float blockType;
in vec3 worldPos;
in vec3 normal;
in vec2 texCoord;

out vec4 FragColor;

uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 ambientLight;

void main()
{
    vec3 color;
    int blockTypeInt = int(round(blockType));

    if (blockTypeInt == 1) color = vec3(0.5, 0.5, 0.5);
    else if (blockTypeInt == 2) color = vec3(0.6, 0.4, 0.2);
    else if (blockTypeInt == 3) color = vec3(0.2, 0.8, 0.2);
    else if (blockTypeInt == 4) color = vec3(0.8, 0.6, 0.4);
    else if (blockTypeInt == 5) color = vec3(0.1, 0.6, 0.1);
    else color = vec3(0.7, 0.7, 0.7);

    float checker = step(0.5, mod(floor(worldPos.x) + floor(worldPos.z), 2.0));
    color = mix(color * 0.9, color * 1.1, checker);

    vec3 lightNormal = normalize(normal);
    float diffuse = max(dot(lightNormal, -normalize(lightDir)), 0.0);
    vec3 finalColor = color * (ambientLight + lightColor * diffuse);

    FragColor = vec4(finalColor, 1.0);
}";
}