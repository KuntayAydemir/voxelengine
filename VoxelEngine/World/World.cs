using System.Collections.Generic;
using OpenTK.Mathematics;
using VoxelEngine.Rendering;

namespace VoxelEngine.World;

public class GameWorld  // World yerine GameWorld (namespace çakışmasını önlemek için)
{
    private int _seed;
    private Dictionary<(int, int), Chunk> _chunks = new();

    public GameWorld(int seed)
    {
        _seed = seed;

        // Örnek olarak 3x3 chunk oluştur
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                // Chunk constructor'ına göre uyarlayın:
                // Vector3 bekliyorsa:
                _chunks[(x, z)] = new Chunk(new Vector3(x, 0, z));
                
                // Vector2i bekliyorsa (ve Chunk bunu destekliyorsa):
                // _chunks[(x, z)] = new Chunk(new Vector2i(x, z));
            }
        }
    }

    public void Update(Vector3 playerPosition)
    {
        foreach (var chunk in _chunks.Values)
        {
            // Chunk mesh güncelleme (eğer gerekiyorsa)
            // chunk.UpdateMesh() çağrısı Chunk.Render() içinde yapılıyor
        }
    }

    public void Render(Shader shader, Matrix4 view, Matrix4 projection)
    {
        shader.Use();
        shader.SetMatrix4("view", view);
        shader.SetMatrix4("projection", projection);
                
        foreach (var chunk in _chunks.Values)
        {
            chunk.Render(shader);  // Shader parametresini ekledik
        }
    }

    // PlayerPhysics tarafından kullanılacak GetBlock metodu
    public BlockType GetBlock(int worldX, int worldY, int worldZ)
    {
        int chunkX = worldX / Chunk.ChunkSize;
        int chunkZ = worldZ / Chunk.ChunkSize;

        int localX = worldX % Chunk.ChunkSize;
        int localZ = worldZ % Chunk.ChunkSize;

        // Negative coordinates için düzeltme
        if (worldX < 0)
        {
            chunkX = (worldX + 1) / Chunk.ChunkSize - 1;
            localX = worldX - chunkX * Chunk.ChunkSize;
        }
        if (worldZ < 0)
        {
            chunkZ = (worldZ + 1) / Chunk.ChunkSize - 1;
            localZ = worldZ - chunkZ * Chunk.ChunkSize;
        }

        if (_chunks.TryGetValue((chunkX, chunkZ), out var chunk))
        {
            return chunk.GetBlock(localX, worldY, localZ);
        }

        return BlockType.Air;
    }
}