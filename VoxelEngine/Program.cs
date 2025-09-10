using VoxelEngine.Core;

class Program
{
    static void Main(string[] args)
    {
        using (var game = new GameWindow())
        {
            game.Run();
        }
    }
}