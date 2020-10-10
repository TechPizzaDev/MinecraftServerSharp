
namespace MCServerSharp.World
{
    public class BlockState
    {
        public static BlockState Air { get; } = new BlockState(0);

        public uint Id { get; }

        public BlockState(uint id)
        {
            Id = id;
        }
    }
}
