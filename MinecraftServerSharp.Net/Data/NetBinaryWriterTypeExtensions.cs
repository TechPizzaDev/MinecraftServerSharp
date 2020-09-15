using System;
using System.Buffers.Binary;
using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.Data
{
    public static class NetBinaryWriterTypeExtensions
    {
        public static void Write(this NetBinaryWriter writer, Chat chat)
        {
            writer.Write(chat.Value);
        }

        public static void Write(this NetBinaryWriter writer, Angle angle)
        {
            writer.Write(angle.Value);
        }

        public static void Write(this NetBinaryWriter writer, Position position)
        {
            writer.Write((long)position.Value);
        }

        public static void Write(this NetBinaryWriter writer, UUID uuid)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong) * 2];
            if (writer.Options.IsBigEndian)
            {
                BinaryPrimitives.WriteUInt64BigEndian(buffer, uuid.X);
                BinaryPrimitives.WriteUInt64BigEndian(buffer.Slice(sizeof(ulong)), uuid.Y);
            }
            else
            {
                throw new NotImplementedException();
                BinaryPrimitives.WriteUInt64LittleEndian(buffer, uuid.X);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(sizeof(ulong)), uuid.Y);
            }
            writer.Write(buffer);
        }
    }
}
