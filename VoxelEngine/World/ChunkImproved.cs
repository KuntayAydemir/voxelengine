using OpenTK.Mathematics;
using VoxelEngine.Rendering;

namespace VoxelEngine.World;

public class ChunkImproved
{
    public const int ChunkSize = 16;
    private int _chunkX, _chunkZ;
    private int _seed;

    private BlockType[,,] _blocks = new BlockType[ChunkSize, 256, ChunkSize];

    public ChunkImproved(int chunkX, int chunkZ, int seed)
    {
        _chunkX = chunkX;
        _chunkZ = chunkZ;
        _seed = seed;

        GenerateBlocks();
    }

    private void GenerateBlocks()
    {
        for (int x = 0; x < ChunkSize; x++)
            for (int z = 0; z < ChunkSize; z++)
                for (int y = 0; y < 256; y++)
                    _blocks[x, y, z] = y <= 50 ? BlockType.Dirt : (y == 51 ? BlockType.Grass : BlockType.Air);
    }

    public void Update(Vector3 playerPosition)
    {
        // Gerektiğinde chunk güncelleme
    }

    public void Render(Shader shader, Matrix4 view, Matrix4 projection)
    {
        // Mesh çizimi
    }

    public BlockType GetBlock(int localX, int y, int localZ)
    {
        if (localX < 0 || localX >= ChunkSize || y < 0 || y >= 256 || localZ < 0 || localZ >= ChunkSize)
            return BlockType.Air;

        return _blocks[localX, y, localZ];
    }
}
