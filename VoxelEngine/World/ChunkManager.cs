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
        private readonly ConcurrentQueue<Vector2i> _unloadQueue = new();
        private readonly GameWorld _world;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        private Task? _generationTask;
        private Task? _meshingTask;
        
        // Configurable settings - performance optimized
        public int RenderDistance { get; set; } = 6; // Başlangıç değeri düşürüldü: 13x13 chunks
        public int UnloadDistance { get; set; } = 10; // Unload mesafesi render distance'dan büyük
        public int MaxChunksPerFrame { get; set; } = 1; // Frame drop'ları azaltmak için
        
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
                // ConcurrentQueue'da Contains yok, basitçe duplicate request'lere izin ver
                // Background thread'de duplicate check yapılacak
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
                        // Duplicate check - chunk zaten var mı?
                        if (_chunks.ContainsKey(position))
                            continue;
                            
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
                    await Task.Delay(50, _cancellationTokenSource.Token); // Daha az aggressive - frame drop'ları azalt
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
            // Main thread'de çağırılacak - GPU workload'ı kontrol et
            int processed = 0;
            while (_meshingQueue.TryDequeue(out var chunk) && processed < MaxChunksPerFrame)
            {
                chunk.MarkMeshForUpdate();
                processed++;
            }
        }
        
        public void UpdateNearPlayer(Vector3 playerPosition)
        {
            int playerChunkX = (int)Math.Floor(playerPosition.X / Chunk.ChunkSize);
            int playerChunkZ = (int)Math.Floor(playerPosition.Z / Chunk.ChunkSize);
            
            // Circular loading pattern - daha doğal görünüm
            for (int x = -RenderDistance; x <= RenderDistance; x++)
            {
                for (int z = -RenderDistance; z <= RenderDistance; z++)
                {
                    // Circular distance check
                    float distance = (float)Math.Sqrt(x * x + z * z);
                    if (distance <= RenderDistance)
                    {
                        var chunkPos = new Vector2i(playerChunkX + x, playerChunkZ + z);
                        RequestChunk(chunkPos);
                    }
                }
            }
        }
        
        public void UnloadDistantChunks(Vector3 playerPosition)
        {
            int playerChunkX = (int)Math.Floor(playerPosition.X / Chunk.ChunkSize);
            int playerChunkZ = (int)Math.Floor(playerPosition.Z / Chunk.ChunkSize);
            
            var chunksToUnload = new List<Vector2i>();
            
            foreach (var kvp in _chunks)
            {
                var chunkPos = kvp.Key;
                float distance = Vector2.Distance(
                    new Vector2(chunkPos.X, chunkPos.Y),
                    new Vector2(playerChunkX, playerChunkZ));
                
                if (distance > UnloadDistance)
                {
                    chunksToUnload.Add(chunkPos);
                }
            }
            
            // Unload chunks
            foreach (var chunkPos in chunksToUnload)
            {
                if (_chunks.TryRemove(chunkPos, out var chunk))
                {
                    chunk?.Dispose();
                }
            }
        }
        
        public void Dispose()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                
                // Try to wait for tasks to complete gracefully
                try
                {
                    _generationTask?.Wait(100); // Short timeout
                }
                catch (AggregateException) { } // Ignore cancellation exceptions
                
                try
                {
                    _meshingTask?.Wait(100); // Short timeout
                }
                catch (AggregateException) { } // Ignore cancellation exceptions
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                
                // Clean up remaining chunks
                foreach (var chunk in _chunks.Values)
                {
                    chunk?.Dispose();
                }
                _chunks.Clear();
            }
        }
    }
}
