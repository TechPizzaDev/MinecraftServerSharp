
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.RecipeBookData)]
    public readonly struct ClientRecipeBookData
    {
        public enum DataType
        {
            DisplayedRecipe = 0,
            RecipeBookStates = 1,
        }

        public DataType Type { get; }

        public Identifier RecipeId { get; }

        public bool CraftingRecipeBookOpen { get; }
        public bool CraftingRecipeFilterActive { get; }
        public bool SmeltingRecipeBookOpen { get; }
        public bool SmeltingRecipeFilterActive { get; }
        public bool BlastingRecipeBookOpen { get; }
        public bool BlastingRecipeFilterActive { get; }
        public bool SmokingRecipeBookOpen { get; }
        public bool SmokingRecipeFilterActive { get; }

        // TODO:

        //[PacketConstructor]
        public ClientRecipeBookData(
            [PacketSwitchCase(DataType.DisplayedRecipe)] DataType type, 
            Identifier recipeId) : this()
        {
            Type = type;
            RecipeId = recipeId;
        }
        
        //[PacketConstructor]
        public ClientRecipeBookData(
            [PacketSwitchCase(DataType.RecipeBookStates)] DataType type,
            bool craftingRecipeBookOpen, bool craftingRecipeFilterActive,
            bool smeltingRecipeBookOpen, bool smeltingRecipeFilterActive,
            bool blastingRecipeBookOpen, bool blastingRecipeFilterActive,
            bool smokingRecipeBookOpen, bool smokingRecipeFilterActive) : this()
        {
            Type = type;
            CraftingRecipeBookOpen = craftingRecipeBookOpen;
            CraftingRecipeFilterActive = craftingRecipeFilterActive;
            SmeltingRecipeBookOpen = smeltingRecipeBookOpen;
            SmeltingRecipeFilterActive = smeltingRecipeFilterActive;
            BlastingRecipeBookOpen = blastingRecipeBookOpen;
            BlastingRecipeFilterActive = blastingRecipeFilterActive;
            SmokingRecipeBookOpen = smokingRecipeBookOpen;
            SmokingRecipeFilterActive = smokingRecipeFilterActive;
        }
    }
}
