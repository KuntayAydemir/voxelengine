using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelEngine.Rendering;

namespace VoxelEngine.World;

public class Chunk
{
    public const int ChunkSize = 16;
    public const int ChunkHeight = 128;

    private readonly BlockType[,,] _blocks = new BlockType[ChunkSize, ChunkHeight, ChunkSize];
    public Vector3 Position;

    private int _vao;
    private int _vbo;
    private int _ebo;
    private int _indexCount;

    public Chunk(Vector3 position)
    {
        Position = position;
        GenerateFlat();
        GenerateMesh();
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

    private void GenerateMesh()
    {
        var vertices = new List<float>();
        var indices = new List<uint>();
        uint indexOffset = 0;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkHeight; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    if (_blocks[x, y, z] == BlockType.Air) continue;

                    AddCube(vertices, indices, x, y, z, (float)_blocks[x, y, z], ref indexOffset);
                }
            }
        }

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, 4 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        _indexCount = indices.Count;
    }

    private void AddCube(List<float> vertices, List<uint> indices, int x, int y, int z, float blockType, ref uint indexOffset)
    {
        var corners = new Vector3[]
        {
            new(x,     y,     z),     // 0
            new(x + 1, y,     z),     // 1
            new(x + 1, y + 1, z),     // 2
            new(x,     y + 1, z),     // 3
            new(x,     y,     z + 1), // 4
            new(x + 1, y,     z + 1), // 5
            new(x + 1, y + 1, z + 1), // 6
            new(x,     y + 1, z + 1)  // 7
        };

        foreach (var corner in corners)
        {
            vertices.Add(corner.X);
            vertices.Add(corner.Y);
            vertices.Add(corner.Z);
            vertices.Add(blockType);
        }

        var faces = new uint[][]
        {
            new uint[] {0, 1, 2, 2, 3, 0},
            new uint[] {1, 5, 6, 6, 2, 1},
            new uint[] {5, 4, 7, 7, 6, 5},
            new uint[] {4, 0, 3, 3, 7, 4},
            new uint[] {3, 2, 6, 6, 7, 3},
            new uint[] {4, 5, 1, 1, 0, 4}
        };

        foreach (var face in faces)
        {
            foreach (var index in face)
            {
                indices.Add(index + indexOffset);
            }
        }

        indexOffset += 8;
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkHeight || z < 0 || z >= ChunkSize)
            return BlockType.Air;
        return _blocks[x, y, z];
    }

    public void Render(Shader shader)
    {
        if (_indexCount == 0) return;

        Matrix4 model = Matrix4.CreateTranslation(Position);
        shader.SetMatrix4("model", model);

        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
    }
}