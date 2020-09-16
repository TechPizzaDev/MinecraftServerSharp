
namespace MCServerSharp.Data
{
    public enum DiggingStatus
    {
        StartedDigging = 0,

        /// <summary>
        /// Sent when the player lets go of the Mine Block key (default: left click)
        /// </summary>
        CancelledDigging = 1,

        /// <summary>
        /// Sent when the client thinks it is finished.
        /// </summary>
        FinishedDigging = 2,

        /// <summary>
        /// Triggered by using the Drop Item key (default: Q) with the modifier to 
        /// drop the entire selected stack (default: depends on OS). 
        /// Location is always set to 0/0/0, Face is always set to -Y.
        /// </summary>
        DropItemStack = 3,

        /// <summary>
        /// Triggered by using the Drop Item key (default: Q). 
        /// Location is always set to 0/0/0, Face is always set to -Y.
        /// </summary>
        DropItem = 4,

        /// <summary>
        /// Indicates that the currently held item should have its state updated such as 
        /// eating food, pulling back bows, using buckets, etc.
        /// Location is always set to 0/0/0, Face is always set to -Y.
        /// </summary>
        UseItem = 5,

        /// <summary>
        /// Used to swap or assign an item to the second hand.
        /// Location is always set to 0/0/0, Face is always set to -Y.
        /// </summary>
        SwapItemInHand = 6
    }
}
