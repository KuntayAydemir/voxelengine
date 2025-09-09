using OpenTK.Mathematics;
using VoxelEngine.Rendering;


namespace VoxelEngine.World;

public class Chunk
{
    public const int ChunkSize = 16;
    public const int ChunkHeight = 128;

    private readonly BlockType[,,] _blocks = new BlockType[ChunkSize, ChunkHeight, ChunkSize];
    public Vector3 Position; // Chunk world position

    public Chunk(Vector3 position)
    {
        Position = position;
        GenerateFlat();
    }

    private void GenerateFlat()
    {
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                for (int y = 0; y < ChunkHeight; y++)
                {
                    if (y < 10)
                        _blocks[x, y, z] = BlockType.Stone;
                    else if (y < 12)
                        _blocks[x, y, z] = BlockType.Dirt;
                    else if (y == 12)
                        _blocks[x, y, z] = BlockType.Grass;
                    else
                        _blocks[x, y, z] = BlockType.Air;
                }
            }
        }
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkHeight || z < 0 || z >= ChunkSize)
            return BlockType.Air;
        return _blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkHeight || z < 0 || z >= ChunkSize)
            return;
        _blocks[x, y, z] = type;
    }

    public void Render(Shader shader)
    {
        // Burada sadece placeholder: gerçek mesh render için shader ve VAO kullanılacak
        // Şimdilik hiçbir şey yapma
    }
}
