using System.Buffers;
using MinecraftServerSharp.Data.IO;
using MinecraftServerSharp.NBT;
using MinecraftServerSharp.Net.Packets;

namespace MinecraftServerSharp.Data
{
    public static class NetBinaryReaderTypeExtensions
    {
        public static OperationStatus Read(this NetBinaryReader reader, out Position position)
        {
            var status = reader.Read(out long rawValue);
            if (status != OperationStatus.Done)
            {
                position = default;
                return status;
            }
            position = new Position((ulong)rawValue);
            return OperationStatus.Done;
        }

        public static OperationStatus Read(this NetBinaryReader reader, out Slot slot)
        {
            var status = reader.Read(out bool present);
            if (status != OperationStatus.Done)
                goto NotDone;

            if (present)
            {
                status = reader.Read(out VarInt itemID);
                if (status != OperationStatus.Done)
                    goto NotDone;

                status = reader.Read(out byte itemCount);
                if (status != OperationStatus.Done)
                    goto NotDone;

                status = reader.Read(out NbtDocument? nbt);
                if (status != OperationStatus.Done)
                    goto NotDone;

                slot = new Slot(itemID, itemCount, nbt);
                return OperationStatus.Done;
            }
            else
            {
                slot = Slot.Empty;
                return OperationStatus.Done;
            }

            NotDone:
            slot = default;
            return status;
        }
    }
}
