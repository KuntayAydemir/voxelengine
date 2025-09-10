using System.Collections.Generic;
using OpenTK.Mathematics;
using VoxelEngine.Rendering;

namespace VoxelEngine.World
{
    public class GameWorld
    {
        public List<Chunk> Chunks = new();

        public GameWorld()
        {
            GenerateChunks();
        }

        public void GenerateChunks()
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var chunk = new Chunk(new Vector3(x * 16, 0, z * 16));
                    Chunks.Add(chunk);
                }
            }
        }

        public BlockType GetBlock(Vector3 position)
        {
            int chunkX = (int)Math.Floor(position.X / 16);
            int chunkZ = (int)Math.Floor(position.Z / 16);

            var chunk = Chunks.Find(c =>
                c.Position.X == chunkX * 16 &&
                c.Position.Z == chunkZ * 16);

            if (chunk == null) return BlockType.Air;

            int localX = (int)(position.X - chunk.Position.X);
            int localY = (int)position.Y;
            int localZ = (int)(position.Z - chunk.Position.Z);

            if (localX < 0 || localX >= Chunk.ChunkSize ||
                localY < 0 || localY >= Chunk.ChunkHeight ||
                localZ < 0 || localZ >= Chunk.ChunkSize)
                return BlockType.Air;

            return chunk.GetBlock(localX, localY, localZ);
        }

        public void Update(float deltaTime)
        {
            // Boş — sonra eklenebilir
        }

        public void Render(Shader shader)
        {
            foreach (var chunk in Chunks)
            {
                chunk.Render(shader);
            }
        }
    }
}