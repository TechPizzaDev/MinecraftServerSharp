using System;
using System.Collections.Concurrent;
using System.Reflection;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public abstract class NbTag
    {
        private static MethodInfo GetTypeMethod { get; }
        private static ConcurrentDictionary<Type, NbtType> TypeMap { get; }

        public Utf8String? Name { get; }

        public abstract NbtType Type { get; }

        // TODO: system.string constructor

        static NbTag()
        {
            GetTypeMethod = typeof(NbTag).GetMethod(nameof(GetNbtType), 1, Array.Empty<Type>())!;
            if (GetTypeMethod == null)
            {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                // This should not throw.
                throw new Exception("Failed to get method required for reflection.");
#pragma warning restore CA1065
            }
            TypeMap = new ConcurrentDictionary<Type, NbtType>();
        }

        public NbTag(Utf8String? name)
        {
            Name = name;
        }

        public virtual void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            if (flags.HasFlag(NbtFlags.Typed))
                writer.Write((byte)Type);

            if (flags.HasFlag(NbtFlags.Named) && Name != null)
            {
                writer.Write((ushort)Name.Length);
                writer.WriteRaw(Name);
            }
        }

        public static NbtType GetNbtType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var nbtType = TypeMap.GetOrAdd(type, (t) =>
            {
                var obj = GetTypeMethod.MakeGenericMethod(t).Invoke(null, null);
                return (NbtType)obj!;
            });
            return nbtType;
        }

        public static NbtType GetNbtType<TTag>()
            where TTag : NbTag
        {
            if (typeof(TTag) == typeof(NbtByte))
                return NbtType.Byte;
            if (typeof(TTag) == typeof(NbtShort))
                return NbtType.Short;
            if (typeof(TTag) == typeof(NbtInt))
                return NbtType.Int;
            if (typeof(TTag) == typeof(NbtLong))
                return NbtType.Long;

            if (typeof(TTag) == typeof(NbtFloat))
                return NbtType.Float;
            if (typeof(TTag) == typeof(NbtDouble))
                return NbtType.Double;

            if (typeof(TTag) == typeof(NbtString))
                return NbtType.String;
            if (typeof(TTag) == typeof(NbtByteArray))
                return NbtType.ByteArray;
            if (typeof(TTag) == typeof(NbtIntArray))
                return NbtType.IntArray;
            if (typeof(TTag) == typeof(NbtLongArray))
                return NbtType.LongArray;

            if (typeof(TTag) == typeof(NbtCompound))
                return NbtType.Compound;
            if (typeof(TTag).GetGenericTypeDefinition() == typeof(NbtList<>))
                return NbtType.List;

            throw new ArgumentException("No matching NBT type for " + typeof(NbTag) + ".");
        }
    }
}
