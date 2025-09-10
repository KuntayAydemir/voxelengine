using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace VoxelEngine.World
{
    public class ChunkManager
    {
        private readonly ConcurrentDictionary<Vector2i, Chunk> _chunks = new();
        private readonly ConcurrentQueue<Vector2i> _generationQueue = new();
        private readonly ConcurrentQueue<Chunk> _meshingQueue = new();
        private readonly GameWorld _world;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        private Task? _generationTask;
        private Task? _meshingTask;
        
        public IEnumerable<Chunk> LoadedChunks => _chunks.Values;
        
        public ChunkManager(GameWorld world)
        {
            _world = world;
            StartBackgroundTasks();
        }
        
        private void StartBackgroundTasks()
        {
            _generationTask = Task.Run(GenerationWorker, _cancellationTokenSource.Token);
            _meshingTask = Task.Run(MeshingWorker, _cancellationTokenSource.Token);
        }
        
        public void RequestChunk(Vector2i position)
        {
            if (!_chunks.ContainsKey(position))
            {
                _generationQueue.Enqueue(position);
            }
        }
        
        public Chunk? GetChunk(Vector2i position)
        {
            _chunks.TryGetValue(position, out var chunk);
            return chunk;
        }
        
        private async Task GenerationWorker()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_generationQueue.TryDequeue(out var position))
                {
                    try
                    {
                        // Background thread'de sadece terrain generate et, OpenGL yok
                        var chunk = CreateChunkData(position);
                        _chunks[position] = chunk;
                        
                        // Mesh update main thread'de yapılacağını işaretle
                        _meshingQueue.Enqueue(chunk);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Chunk generation error: {ex.Message}");
                    }
                }
                else
                {
                    await Task.Delay(16, _cancellationTokenSource.Token); // ~60 FPS check
                }
            }
        }
        
        private Chunk CreateChunkData(Vector2i position)
        {
            // Sadece data generation, OpenGL calls yok
            var chunk = new Chunk(position, generateMeshImmediately: false);
            return chunk;
        }

        private async Task MeshingWorker()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Mesh update'ler main thread'de yapılmalı (OpenGL context)
                await Task.Delay(16, _cancellationTokenSource.Token);
            }
        }
        
        public void ProcessMeshingQueue()
        {
            // Main thread'de çağırılacak
            int processed = 0;
            while (_meshingQueue.TryDequeue(out var chunk) && processed < 2) // Frame başına max 2 chunk
            {
                chunk.MarkMeshForUpdate();
                processed++;
            }
        }
        
        public void UpdateNearPlayer(Vector3 playerPosition)
        {
            int playerChunkX = (int)Math.Floor(playerPosition.X / 16);
            int playerChunkZ = (int)Math.Floor(playerPosition.Z / 16);
            
            const int renderDistance = 3;
            
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    var chunkPos = new Vector2i(playerChunkX + x, playerChunkZ + z);
                    RequestChunk(chunkPos);
                }
            }
        }
        
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _generationTask?.Wait(1000);
            _meshingTask?.Wait(1000);
            _cancellationTokenSource.Dispose();
        }
    }
}
