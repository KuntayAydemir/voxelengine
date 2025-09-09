using System;
using System.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using VoxelEngine.Core;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            // Parse command line arguments
            bool debug = args.Contains("--debug");
            bool fullscreen = args.Contains("--fullscreen");
            int seed = 1337;
            var seedArg = args.FirstOrDefault(a => a.StartsWith("--seed="));
            if (seedArg != null)
            {
                var value = seedArg.Substring("--seed=".Length);
                if (int.TryParse(value, out int parsed))
                {
                    seed = parsed;
                }
            }
            
            // Configuration settings
            var gameWindowSettings = GameWindowSettings.Default;
            gameWindowSettings.UpdateFrequency = 60.0;
            
            var nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(1920, 1080),
                Title = "Voxel Engine - godshandproject",
                WindowState = fullscreen ? OpenTK.Windowing.Common.WindowState.Fullscreen : OpenTK.Windowing.Common.WindowState.Normal,
            };

            if (debug)
            {
                Console.WriteLine("Starting Voxel Engine in debug mode...");
                Console.WriteLine($"Window Size: {nativeWindowSettings.ClientSize}");
                Console.WriteLine($"Fullscreen: {fullscreen}");
                Console.WriteLine($"Seed: {seed}");
            }

            using var window = new VoxelEngine.Core.GameWindow(gameWindowSettings, nativeWindowSettings, seed);
            window.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error starting Voxel Engine: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}

