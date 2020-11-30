
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.SetDisplayedRecipe)]
    public readonly struct ClientSetDisplayedRecipe
    {
        public Utf8Identifier RecipeId { get; }

        [PacketConstructor]
        public ClientSetDisplayedRecipe(Utf8Identifier recipeId)
        {
            RecipeId = recipeId;
        }
    }
}
