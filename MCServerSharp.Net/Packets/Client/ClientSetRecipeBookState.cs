using MCServerSharp.Data;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.SetRecipeBookState)]
    public readonly struct ClientSetRecipeBookState
    {
        public RecipeBookId BookId { get; }
        public bool BookOpen { get; }
        public bool FilterActive { get; }

        [PacketConstructor]
        public ClientSetRecipeBookState(VarInt bookId, bool bookOpen, bool filterActive)
        {
            BookId = bookId.AsEnum<RecipeBookId>();
            BookOpen = bookOpen;
            FilterActive = filterActive;
        }
    }
}
