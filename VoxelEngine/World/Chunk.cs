using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelEngine.Rendering;

namespace VoxelEngine.World;

public class Chunk
{
    public const int ChunkSize = 16;
    public const int ChunkHeight = 256;
    
    // Chunk koordinatı (dünya koordinatı değil)
    public Vector2i Position { get; private set; }
    private BlockType[,,] _blocks;
    private int _vao, _vbo;
    private int _vertexCount;
    private bool _meshNeedsUpdate = true;
    private List<float> _vertices = new List<float>();

    public Chunk(Vector2i position, bool generateMeshImmediately = true)
    {
        Position = position;
        _blocks = new BlockType[ChunkSize, ChunkHeight, ChunkSize];
        
        if (generateMeshImmediately)
        {
            // Generate OpenGL buffers (main thread only)
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
        }
        
        GenerateTerrain();
        
        if (!generateMeshImmediately)
        {
            _meshNeedsUpdate = true; // Mesh daha sonra oluşturulacak
        }
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkHeight || z < 0 || z >= ChunkSize)
            return BlockType.Air;
        
        return _blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, BlockType blockType)
    {
        if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkHeight || z < 0 || z >= ChunkSize)
            return;
        
        _blocks[x, y, z] = blockType;
        _meshNeedsUpdate = true;
    }

    private void GenerateTerrain()
    {
        // Prosedürel dünya oluşumu - Perlin noise ile height variation
        const int baseGroundLevel = 50;
        const double noiseScale = 0.02; // Ne kadar 'zoomed in' noise
        const int heightVariation = 15; // Maks yükseklik farkı
        
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                // Dünya koordinatlarını al
                double worldX = Position.X * ChunkSize + x;
                double worldZ = Position.Y * ChunkSize + z;
                
                // Perlin noise ile yükseklik hesapla
                double noiseValue = PerlinNoise.OctaveNoise(worldX, 0, worldZ, 4, 0.5, noiseScale);
                int height = baseGroundLevel + (int)(noiseValue * heightVariation);
                
                // Blokları yerleştir
                for (int y = 0; y <= height && y < ChunkHeight; y++)
                {
                    if (y < height - 5)
                        SetBlock(x, y, z, BlockType.Stone);
                    else if (y < height)
                        SetBlock(x, y, z, BlockType.Dirt);
                    else if (y == height)
                        SetBlock(x, y, z, BlockType.Grass);
                }
                
                // Basit biome sistemi - yüksekliğe göre
                if (height > baseGroundLevel + 8)
                {
                    // Dağlık bölge - taş çıkıntıları
                    if (height < ChunkHeight - 1 && noiseValue > 0.3)
                        SetBlock(x, height + 1, z, BlockType.Stone);
                }
            }
        }
        _meshNeedsUpdate = true;
    }

        private void UpdateMesh()
        {
            if (!_meshNeedsUpdate) return;

            _vertices.Clear();
            _vertices.Capacity = 10000; // Pre-allocate capacity to reduce allocations
            int solidBlocks = 0;

            // Y optimization - sadece terrain olan bölgeleri tara
            int maxY = 80; // Daha da düşürdük terrain generation için
        
        for (int y = 0; y < maxY; y++)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    BlockType block = GetBlock(x, y, z);
                    if (block == BlockType.Air) continue;
                    
                    solidBlocks++;

                    Vector3 worldPos = new Vector3(
                        Position.X * ChunkSize + x,
                        y,
                        Position.Y * ChunkSize + z
                    );

                    // Check each face and add if not occluded
                    AddFaceIfVisible(worldPos, block, x, y, z, Direction.Front);
                    AddFaceIfVisible(worldPos, block, x, y, z, Direction.Back);
                    AddFaceIfVisible(worldPos, block, x, y, z, Direction.Left);
                    AddFaceIfVisible(worldPos, block, x, y, z, Direction.Right);
                    AddFaceIfVisible(worldPos, block, x, y, z, Direction.Top);
                    AddFaceIfVisible(worldPos, block, x, y, z, Direction.Bottom);
                }
            }
        }

        UpdateBuffers();
        _meshNeedsUpdate = false;
    }

    private void AddFaceIfVisible(Vector3 worldPos, BlockType block, int x, int y, int z, Direction direction)
    {
        Vector3i neighborPos = GetNeighborPosition(x, y, z, direction);
        BlockType neighbor = GetBlock(neighborPos.X, neighborPos.Y, neighborPos.Z);

        if (!BlockProperties.IsTransparent(neighbor)) return;

        AddFaceVertices(worldPos, direction, block);
    }

    private Vector3i GetNeighborPosition(int x, int y, int z, Direction direction)
    {
        return direction switch
        {
            Direction.Front => new Vector3i(x, y, z + 1),
            Direction.Back => new Vector3i(x, y, z - 1),
            Direction.Left => new Vector3i(x - 1, y, z),
            Direction.Right => new Vector3i(x + 1, y, z),
            Direction.Top => new Vector3i(x, y + 1, z),
            Direction.Bottom => new Vector3i(x, y - 1, z),
            _ => new Vector3i(x, y, z)
        };
    }

    private void AddFaceVertices(Vector3 pos, Direction direction, BlockType blockType)
    {
        Vector3[] faceVertices = GetFaceVertices(pos, direction);
        Vector3 normal = GetFaceNormal(direction);
        float blockTypeFloat = (float)blockType;
        
        // First triangle (pos, texCoord, normal, blockType)
        _vertices.AddRange(new float[] { faceVertices[0].X, faceVertices[0].Y, faceVertices[0].Z, 0, 0, normal.X, normal.Y, normal.Z, blockTypeFloat });
        _vertices.AddRange(new float[] { faceVertices[1].X, faceVertices[1].Y, faceVertices[1].Z, 1, 0, normal.X, normal.Y, normal.Z, blockTypeFloat });
        _vertices.AddRange(new float[] { faceVertices[2].X, faceVertices[2].Y, faceVertices[2].Z, 1, 1, normal.X, normal.Y, normal.Z, blockTypeFloat });
        
        // Second triangle
        _vertices.AddRange(new float[] { faceVertices[0].X, faceVertices[0].Y, faceVertices[0].Z, 0, 0, normal.X, normal.Y, normal.Z, blockTypeFloat });
        _vertices.AddRange(new float[] { faceVertices[2].X, faceVertices[2].Y, faceVertices[2].Z, 1, 1, normal.X, normal.Y, normal.Z, blockTypeFloat });
        _vertices.AddRange(new float[] { faceVertices[3].X, faceVertices[3].Y, faceVertices[3].Z, 0, 1, normal.X, normal.Y, normal.Z, blockTypeFloat });
    }

    private Vector3[] GetFaceVertices(Vector3 pos, Direction direction)
    {
        return direction switch
        {
            Direction.Front => new[]
            {
                pos + new Vector3(0, 0, 1),
                pos + new Vector3(1, 0, 1),
                pos + new Vector3(1, 1, 1),
                pos + new Vector3(0, 1, 1)
            },
            Direction.Back => new[]
            {
                pos + new Vector3(1, 0, 0),
                pos + new Vector3(0, 0, 0),
                pos + new Vector3(0, 1, 0),
                pos + new Vector3(1, 1, 0)
            },
            Direction.Left => new[]
            {
                pos + new Vector3(0, 0, 0),
                pos + new Vector3(0, 0, 1),
                pos + new Vector3(0, 1, 1),
                pos + new Vector3(0, 1, 0)
            },
            Direction.Right => new[]
            {
                pos + new Vector3(1, 0, 1),
                pos + new Vector3(1, 0, 0),
                pos + new Vector3(1, 1, 0),
                pos + new Vector3(1, 1, 1)
            },
            Direction.Top => new[]
            {
                pos + new Vector3(0, 1, 1),
                pos + new Vector3(1, 1, 1),
                pos + new Vector3(1, 1, 0),
                pos + new Vector3(0, 1, 0)
            },
            Direction.Bottom => new[]
            {
                pos + new Vector3(0, 0, 0),
                pos + new Vector3(1, 0, 0),
                pos + new Vector3(1, 0, 1),
                pos + new Vector3(0, 0, 1)
            },
            _ => new Vector3[4]
        };
    }

    private Vector3 GetFaceNormal(Direction direction)
    {
        return direction switch
        {
            Direction.Front => new Vector3(0, 0, 1),
            Direction.Back => new Vector3(0, 0, -1),
            Direction.Left => new Vector3(-1, 0, 0),
            Direction.Right => new Vector3(1, 0, 0),
            Direction.Top => new Vector3(0, 1, 0),
            Direction.Bottom => new Vector3(0, -1, 0),
            _ => Vector3.Zero
        };
    }

    private void UpdateBuffers()
    {
        // Lazy OpenGL buffer initialization
        if (_vao == 0)
        {
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
        }
        
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        
        // Vertex format: position(3) + texCoord(2) + normal(3) + blockType(1) = 9 floats per vertex
        if (_vertices.Count > 0)
        {
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(float), _vertices.ToArray(), BufferUsageHint.StaticDraw);
        
            // Position attribute (location 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            
            // Texture coordinate attribute (location 1)
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            // Normal attribute (location 2)
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 5 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            
            // Block type attribute (location 3)
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 9 * sizeof(float), 8 * sizeof(float));
            GL.EnableVertexAttribArray(3);
        }
        
        _vertexCount = _vertices.Count / 9;
    }

    public void MarkMeshForUpdate()
    {
        _meshNeedsUpdate = true;
    }
    
    public void Render()
    {
        if (_meshNeedsUpdate) UpdateMesh();
        
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
    }

    public void Dispose()
    {
        if (_vao != 0)
        {
            GL.DeleteVertexArray(_vao);
            _vao = 0;
        }
        
        if (_vbo != 0)
        {
            GL.DeleteBuffer(_vbo);
            _vbo = 0;
        }
        
        _vertices?.Clear();
        _blocks = null!;
    }
}

public static class BlockProperties
{
    public static bool IsTransparent(BlockType type)
    {
        return type == BlockType.Air;
    }
}

public enum Direction
{
    Front, Back, Left, Right, Top, Bottom
}
