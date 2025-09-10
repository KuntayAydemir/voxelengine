using System;
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
            for (int cx = -1; cx <= 1; cx++)
            {
                for (int cz = -1; cz <= 1; cz++)
                {
                    // Chunk, chunk-koordinatlarında Vector2i bekliyor (dünya değil)
                    var chunk = new Chunk(new Vector2i(cx, cz));
                    Chunks.Add(chunk);
                }
            }
        }

        private static int FloorDiv(int a, int b) => (int)Math.Floor((double)a / b);
        private static int ModFloor(int a, int m)
        {
            int r = a % m;
            return r < 0 ? r + m : r;
        }

        public BlockType GetBlock(Vector3 position)
        {
            // Dünya koordinatlarını tamsayıya indir
            int wx = (int)Math.Floor(position.X);
            int wy = (int)Math.Floor(position.Y);
            int wz = (int)Math.Floor(position.Z);

            int chunkX = FloorDiv(wx, Chunk.ChunkSize);
            int chunkZ = FloorDiv(wz, Chunk.ChunkSize);

            var chunk = Chunks.Find(c => c.Position.X == chunkX && c.Position.Y == chunkZ);
            if (chunk == null) return BlockType.Air;

            int localX = ModFloor(wx, Chunk.ChunkSize);
            int localY = wy;
            int localZ = ModFloor(wz, Chunk.ChunkSize);

            if (localX < 0 || localX >= Chunk.ChunkSize ||
                localY < 0 || localY >= Chunk.ChunkHeight ||
                localZ < 0 || localZ >= Chunk.ChunkSize)
                return BlockType.Air;

            return chunk.GetBlock(localX, localY, localZ);
        }

        public void Update(float deltaTime)
        {
            // Boş — sonra eklenebilir (chunk yükleme/boşaltma vb.)
        }

        public void Render(Shader shader)
        {
            foreach (var chunk in Chunks)
            {
                // Shader zaten aktif — chunk kendi VAO/VBO'sunu çizer
                chunk.Render();
            }
        }
    }
}
