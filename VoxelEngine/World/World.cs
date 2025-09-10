using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using VoxelEngine.Rendering;

namespace VoxelEngine.World
{
    public class GameWorld
    {
        private readonly ChunkManager _chunkManager;
        private Vector3 _lastPlayerPosition = Vector3.Zero;
        private const float CHUNK_UPDATE_DISTANCE = 32.0f; // Player'ın en az 2 chunk hareket etmesi gerekli
        
        // Public property for backwards compatibility
        public IEnumerable<Chunk> Chunks => _chunkManager.LoadedChunks;

        public GameWorld()
        {
            _chunkManager = new ChunkManager(this);
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

            var chunk = _chunkManager.GetChunk(new Vector2i(chunkX, chunkZ));
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
            // Oyuncu yeterince hareket ettiyse chunk sistemi güncelle
            float distanceMoved = Vector3.Distance(playerPosition, _lastPlayerPosition);
            if (distanceMoved > CHUNK_UPDATE_DISTANCE)
            {
                _chunkManager.UpdateNearPlayer(playerPosition);
                _chunkManager.UnloadDistantChunks(playerPosition);
                _lastPlayerPosition = playerPosition;
            }
            
            // Her frame mesh queue'yu işle
            _chunkManager.ProcessMeshingQueue();
        }

        public void Render(Shader shader)
        {
            foreach (var chunk in _chunkManager.LoadedChunks)
            {
                // Shader zaten aktif — chunk kendi VAO/VBO'sunu çizer
                chunk.Render();
            }
        }
        
        public void IncreaseRenderDistance()
        {
            if (_chunkManager.RenderDistance < 12) // Max 12 (memory optimized)
            {
                _chunkManager.RenderDistance++;
                _chunkManager.UnloadDistance = _chunkManager.RenderDistance + 4;
                _chunkManager.MaxChunksPerFrame = Math.Max(1, 3 - _chunkManager.RenderDistance / 4); // Yüksek render distance'ta daha az chunk/frame
            }
        }
        
        public void DecreaseRenderDistance()
        {
            if (_chunkManager.RenderDistance > 3) // Min 3
            {
                _chunkManager.RenderDistance--;
                _chunkManager.UnloadDistance = _chunkManager.RenderDistance + 4;
                _chunkManager.MaxChunksPerFrame = Math.Max(1, 3 - _chunkManager.RenderDistance / 4);
            }
        }
        
        public int GetRenderDistance() => _chunkManager.RenderDistance;
        
        public void Dispose()
        {
            _chunkManager?.Dispose();
        }
    }
}
