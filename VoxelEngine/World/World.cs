using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using VoxelEngine.Rendering;

namespace VoxelEngine.World
{
    public class GameWorld
    {
        public List<Chunk> Chunks = new();
        private Vector3 _lastPlayerPosition = Vector3.Zero;

        public GameWorld()
        {
            GenerateChunks();
        }

        public void GenerateChunks()
        {
            // Render distance: 7x7 = 49 chunks
            const int renderDistance = 3;
            for (int cx = -renderDistance; cx <= renderDistance; cx++)
            {
                for (int cz = -renderDistance; cz <= renderDistance; cz++)
                {
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

        public void Update(float deltaTime, Vector3 playerPosition)
        {
            // Şimdilik basit - gelecekte dynamic chunk loading eklenecek
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
