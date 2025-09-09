namespace VoxelEngine.World;
public class Block
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsSolid { get; set; }
    public static readonly Block Air = new Block(0, "Air", false);
    public static readonly Block Dirt = new Block(1, "Dirt", true);
    public static readonly Block Grass = new Block(2, "Grass", true);
    public Block(int id, string name, bool isSolid)
    {
        Id = id;
        Name = name;
        IsSolid = isSolid;
    }
}