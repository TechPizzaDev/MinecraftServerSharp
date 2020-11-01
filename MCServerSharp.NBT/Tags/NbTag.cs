using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public abstract class NbTag
    {
        /// <summary>
        /// Static instance of <see cref="NbtEnd"/> that can be used everywhere.
        /// </summary>
        public static NbtEnd End { get; } = new NbtEnd();

        private static MethodInfo GetTypeMethod { get; }
        private static ConcurrentDictionary<Type, NbtType> TypeMap { get; }

        public abstract NbtType Type { get; }

        static NbTag()
        {
            Expression<Func<NbtType>> getTypeExpression = () => GetNbtType<NbtInt>(); // generic type doesn't matter
            var getTypeMethodExpression = (MethodCallExpression)getTypeExpression.Body;

            GetTypeMethod = getTypeMethodExpression.Method.GetGenericMethodDefinition();
            TypeMap = new ConcurrentDictionary<Type, NbtType>();
        }

        // TODO: system.string constructor

        public NbTag()
        {
        }

        public virtual void WriteHeader(NetBinaryWriter writer, NbtFlags flags)
        {
            if (flags.HasFlag(NbtFlags.Typed))
                writer.Write((byte)Type);
        }

        public abstract void WritePayload(NetBinaryWriter writer, NbtFlags flags);

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
