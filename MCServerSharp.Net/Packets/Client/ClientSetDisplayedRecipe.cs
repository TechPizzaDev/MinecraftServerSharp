
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.SetDisplayedRecipe)]
    public readonly struct ClientSetDisplayedRecipe
    {
        public Identifier RecipeId { get; }

        [PacketConstructor]
        public ClientSetDisplayedRecipe(Identifier recipeId)
        {
            RecipeId = recipeId;
        }
    }
}
