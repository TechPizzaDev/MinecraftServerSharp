using System.Buffers;

namespace MCServerSharp.NBT
{
    public enum NbtReadStatus
    {
        Done,
        NeedMoreData = OperationStatus.NeedMoreData,
        InvalidData = OperationStatus.InvalidData,
        EndOfDocument,

        InvalidNameLength,

        InvalidStringLength,

        InvalidArrayLength,
        InvalidArrayLengthBytes,

        /// <summary>
        /// <see cref="NbtType.List"/> length was less than or equal to zero
        /// but the type was not <see cref="NbtType.End"/>.
        /// </summary>
        InvalidListLength,

        InvalidListLengthBytes,

        UnknownTag,
    }
}
