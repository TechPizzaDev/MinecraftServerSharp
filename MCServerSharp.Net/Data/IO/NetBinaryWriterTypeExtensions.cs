using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace MCServerSharp.Data.IO
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

        public static void Write(this NetBinaryWriter writer, Identifier identifier)
        {
            writer.WriteUtf8(identifier.Value);
        }

        public static void Write(this NetBinaryWriter writer, Utf8Identifier identifier)
        {
            writer.Write(identifier.Value);
        }

        [SkipLocalsInit]
        public static void Write(this NetBinaryWriter writer, UUID uuid)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong) * 2];
            if (writer.Options.IsBigEndian)
            {
                BinaryPrimitives.WriteUInt64BigEndian(buffer, uuid.X);
                BinaryPrimitives.WriteUInt64BigEndian(buffer[sizeof(ulong)..], uuid.Y);
            }
            else
            {
                throw new NotImplementedException();
                BinaryPrimitives.WriteUInt64LittleEndian(buffer, uuid.X);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer[sizeof(ulong)..], uuid.Y);
            }
            writer.Write(buffer);
        }
    }
}
