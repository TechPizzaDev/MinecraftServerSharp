
namespace MCServerSharp.World
{
    public enum ChunkStatus
    {
        /// <summary>
        /// The chunk is in an undefined state.
        /// </summary>
        Unknown,

        /// <summary>
        /// The chunk has not been requested or has been unloaded.
        /// </summary>
        Unloaded,

        /// <summary>
        /// The chunk is being enqueued to a system.
        /// </summary>
        Pending,

        /// <summary>
        /// The chunk is queued for load on a designated system.
        /// </summary>
        Queued,

        /// <summary>
        /// The chunk is generating terrain.
        /// </summary>
        Generating,

        /// <summary>
        /// Thch chunk is being populated after terrain generation.
        /// </summary>
        Populating,

        /// <summary>
        /// The chunk is being transferred between systems.
        /// </summary>
        Transition,

        /// <summary>
        /// The chunk is loaded.
        /// </summary>
        Loaded
    }
}
