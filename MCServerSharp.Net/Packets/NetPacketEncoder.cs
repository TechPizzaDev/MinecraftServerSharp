using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using MCServerSharp.Collections;
using MCServerSharp.Data.IO;
using MCServerSharp.NBT;

// TODO: turn reflection into Source Generator

namespace MCServerSharp.Net.Packets
{
    public delegate void NetPacketWriterAction<TPacket>(
        NetBinaryWriter writer, in TPacket packet);

    /// <summary>
    /// Gives access to delegates that turn packets into network messages.
    /// </summary>
    public partial class NetPacketEncoder : NetPacketCoder<ServerPacketId>
    {
        private static Type[] _binaryWriterWriteMethodSources = new[]
        {
            typeof(NetBinaryWriter),
            typeof(NetBinaryWriterTypeExtensions),
            typeof(NetBinaryWriterNbtExtensions),
        };

        public NetPacketEncoder() : base()
        {
            RegisterDataTypes();
        }

        #region RegisterDataType[s]

        protected override void RegisterDataType(params Type[] arguments)
        {
            RegisterDataTypeFromMethod(_binaryWriterWriteMethodSources, "Write", arguments);
        }

        protected virtual void RegisterDataTypes()
        {
            // TODO: add attribute for auto-registering

            RegisterDataType(typeof(bool));
            RegisterDataType(typeof(sbyte));
            RegisterDataType(typeof(byte));
            RegisterDataType(typeof(short));
            RegisterDataType(typeof(ushort));
            RegisterDataType(typeof(int));
            RegisterDataType(typeof(long));
            RegisterDataType(typeof(VarInt));
            RegisterDataType(typeof(VarLong));
            RegisterDataType(typeof(float));
            RegisterDataType(typeof(double));
            RegisterDataType(typeof(Utf8String));
            RegisterDataType(typeof(Utf8Memory));
            RegisterDataType(typeof(string));

            RegisterDataType(typeof(NetBinaryWriter), typeof(Chat));
            RegisterDataType(typeof(NetBinaryWriter), typeof(Angle));
            RegisterDataType(typeof(NetBinaryWriter), typeof(Position));
            RegisterDataType(typeof(NetBinaryWriter), typeof(Identifier));
            RegisterDataType(typeof(NetBinaryWriter), typeof(Utf8Identifier));
            RegisterDataType(typeof(NetBinaryWriter), typeof(UUID));
            RegisterDataType(typeof(NetBinaryWriter), typeof(NbTag));
            RegisterDataType(typeof(NetBinaryWriter), typeof(NbtCompound));
        }

        #endregion

        public void RegisterServerPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.Attribute.IsServerPacket);
        }

        public NetPacketWriterAction<TPacket> GetPacketWriterAction<TPacket>()
        {
            return (NetPacketWriterAction<TPacket>)GetPacketAction(typeof(TPacket));
        }

        private static void TryAddDisposableCall(ParameterExpression packetParam, List<Expression> expressions)
        {
            if (!typeof(IDisposable).IsAssignableFrom(packetParam.Type))
            {
                return;
            }

            var disposeMethod = packetParam.Type.GetMethod(nameof(IDisposable.Dispose));
            if (disposeMethod == null)
            {
                throw new Exception(
                    $"Failed to get public {nameof(IDisposable.Dispose)} method required for reflection.");
            }
            var disposeCall = Expression.Call(packetParam, disposeMethod);
            expressions.Add(disposeCall);
        }

        public override Delegate CreatePacketAction(PacketStructInfo structInfo)
        {
            // TODO: add support IDisposable

            var expressions = new List<Expression>();
            var writerParam = Expression.Parameter(typeof(NetBinaryWriter), "Writer");
            var packetParam = Expression.Parameter(structInfo.Type.MakeByRefType(), "Packet");

            if (typeof(IDataWritable).IsAssignableFrom(structInfo.Type))
            {
                string methodName = nameof(IDataWritable.WriteTo);
                var writeMethod = structInfo.Type.GetMethod(methodName, new[] { writerParam.Type });
                if (writeMethod == null)
                {
                    throw new Exception(
                        $"Failed to get public {nameof(IDataWritable.WriteTo)} method required for reflection.");
                }
                var writeCall = Expression.Call(packetParam, writeMethod, writerParam);
                expressions.Add(writeCall);
            }
            else
            {
                ReflectiveWrite(expressions, writerParam, packetParam);
            }

            TryAddDisposableCall(packetParam, expressions);

            var actionType = typeof(NetPacketWriterAction<>).MakeGenericType(structInfo.Type);
            var lambdaBody = Expression.Block(expressions);
            var lambdaArgs = new[] { writerParam, packetParam };
            var resultLambda = Expression.Lambda(actionType, lambdaBody, lambdaArgs);
            return resultLambda.Compile();
        }

        private static DataSerializeMode PickModeByType(DataSerializeMode mode, Type elementType)
        {
            if (mode == DataSerializeMode.Auto)
            {
                if (elementType == typeof(bool) ||
                    elementType == typeof(sbyte) ||
                    elementType == typeof(byte) ||
                    elementType == typeof(short) ||
                    elementType == typeof(ushort) ||
                    elementType == typeof(int) ||
                    elementType == typeof(uint) ||
                    elementType == typeof(long) ||
                    elementType == typeof(ulong) ||
                    elementType == typeof(float) ||
                    elementType == typeof(double))
                {
                    return DataSerializeMode.Copy;
                }
                else
                {
                    return DataSerializeMode.Serialize;
                }
            }
            return mode;
        }

        private void ReflectiveWrite(
            List<Expression> expressions,
            ParameterExpression writerParam,
            Expression instance)
        {
            var publicProps = instance.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var attributedProps = publicProps.SelectWhere(
                p => p.GetCustomAttribute<DataPropertyAttribute>(),
                (p, a) => a != null,
                (prop, propAttrib) =>
            {
                var lengthConstraintAttrib = prop.GetCustomAttribute<DataLengthConstraintAttribute>();
                return new DataPropertyInfo(prop, propAttrib!, lengthConstraintAttrib);
            });

            List<DataPropertyInfo> propList = attributedProps.ToList();
            if (propList.Count == 0)
                return;

            propList.Sort((x, y) => x.Order.CompareTo(y.Order));

            for (int i = 0; i < propList.Count; i++)
            {
                var propInfo = propList[i];
                var propExpresion = Expression.Property(instance, propInfo.Property);

                // TODO: clean this up
                // TODO: adapt for lists?
                // TODO: move expression code to generic methods (like with BlitArray)

                var dataEnumerable = propInfo.Property.GetCustomAttribute<DataEnumerableAttribute>();
                if (dataEnumerable != null)
                {
                    var collectionVariables = new List<ParameterExpression>();
                    var collectionExpressions = new List<Expression>();

                    // Write out collection length.
                    TryApplyLengthPrefix(collectionExpressions, propInfo, writerParam, propExpresion);

                    DataSerializeMode elementMode = dataEnumerable.ElementMode;

                    Type propType = propExpresion.Type;
                    Type propGenericDef = propType.IsGenericType
                        ? propType.GetGenericTypeDefinition()
                        : propType;

                    bool isROSpan = propGenericDef == typeof(ReadOnlySpan<>);
                    bool isSpan = propGenericDef == typeof(Span<>);
                    bool isROMemory = propGenericDef == typeof(ReadOnlyMemory<>);
                    bool isMemory = propGenericDef == typeof(Memory<>);
                    bool isSpanOrMemory = isROSpan || isSpan || isROMemory || isMemory;

                    if (propType.IsArray || isSpanOrMemory)
                    {
                        Type elementType = isSpanOrMemory 
                            ? propType.GenericTypeArguments[0]
                            : propType.GetElementType()!;

                        elementMode = PickModeByType(elementMode, elementType);

                        if (elementMode == DataSerializeMode.Copy)
                        {
                            var blitMethod = BlitSpanMethod.MakeGenericMethod(elementType);

                            var spanField = Expression.Variable(typeof(ReadOnlySpan<>).MakeGenericType(elementType));
                            collectionVariables.Add(spanField);

                            if (isROSpan)
                            {
                                collectionExpressions.Add(Expression.Assign(spanField, propExpresion));
                            }
                            else if (isROMemory)
                            {
                                collectionExpressions.Add(Expression.Assign(
                                    spanField, Expression.Property(propExpresion, "Span")));
                            }
                            else if (isMemory)
                            {
                                collectionExpressions.Add(Expression.Assign(
                                    spanField,
                                    Expression.Convert(
                                        Expression.Property(propExpresion, "Span"), spanField.Type)));
                            }
                            else // if (isSpan || isArray)
                            {
                                collectionExpressions.Add(Expression.Assign(
                                    spanField,
                                    Expression.Convert(propExpresion, spanField.Type)));
                            }

                            var writerCall = Expression.Call(blitMethod, writerParam, spanField);
                            collectionExpressions.Add(writerCall);
                        }
                        else
                        {
                            var arrayIndex = Expression.Variable(typeof(int), "i");
                            collectionVariables.Add(arrayIndex);

                            var writeBody = new List<Expression>();
                            var arrayAccess = Expression.ArrayAccess(propExpresion, arrayIndex);
                            ReflectiveWriteElement(elementMode, writeBody, writerParam, arrayAccess);
                            writeBody.Add(Expression.PostIncrementAssign(arrayIndex));
                            var writeBlock = Expression.Block(writeBody);

                            var arrayLength = Expression.ArrayLength(propExpresion);
                            var checkIndex = Expression.GreaterThanOrEqual(
                                Expression.Convert(arrayIndex, typeof(uint)),
                                Expression.Convert(arrayLength, typeof(uint)));

                            var breakTarget = Expression.Label("End");
                            var checkOrBreak = Expression.IfThenElse(
                                checkIndex,
                                Expression.Break(breakTarget),
                                writeBlock);

                            var arrayLoop = Expression.Loop(checkOrBreak, breakTarget);
                            collectionExpressions.Add(arrayLoop);
                        }
                    }
                    else
                    {
                        var getEnumeratorMethod = propType.GetMethod(
                            "GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
                        if (getEnumeratorMethod == null)
                            throw new Exception($"Property {propExpresion} is missing a \"GetEnumerator\" method.");

                        AssertValidEnumerator(
                            getEnumeratorMethod.ReturnType,
                            out var currentMember,
                            out var moveNextMethod,
                            out var disposeMethod);

                        // Define enumerator variable.
                        var enumeratorVar = Expression.Variable(getEnumeratorMethod.ReturnType, "Enumerator");
                        collectionVariables.Add(enumeratorVar);

                        // Create and assign enumerator to it's variable.
                        var enumeratorCall = Expression.Call(propExpresion, getEnumeratorMethod);
                        var enumeratorAssign = Expression.Assign(enumeratorVar, enumeratorCall);
                        collectionExpressions.Add(enumeratorAssign);

                        var writeBody = new List<Expression>();
                        var current = Expression.MakeMemberAccess(enumeratorVar, currentMember);
                        ReflectiveWriteElement(elementMode, writeBody, writerParam, current);
                        var writeBlock = Expression.Block(writeBody);

                        var breakTarget = Expression.Label("End");
                        var moveNextOrBreak = Expression.IfThenElse(
                            Expression.Call(enumeratorVar, moveNextMethod),
                            writeBlock,
                            Expression.Break(breakTarget));

                        var enumeratorLoop = Expression.Loop(moveNextOrBreak, breakTarget);
                        if (disposeMethod != null)
                        {
                            var finallyDispose = Expression.Call(enumeratorVar, disposeMethod);
                            var tryFinally = Expression.TryFinally(enumeratorLoop, finallyDispose);
                            collectionExpressions.Add(tryFinally);
                        }
                        else
                        {
                            collectionExpressions.Add(enumeratorLoop);
                        }
                    }

                    var collectionBlock = Expression.Block(collectionVariables, collectionExpressions);
                    expressions.Add(collectionBlock);
                }
                else
                {
                    // TODO: Call WriteElement first as it may throw more descriptive errors.
                    var writeExpressions = new List<Expression>();
                    ReflectiveWriteElement(
                        propInfo.PropertyAttrib.SerializeMode, writeExpressions, writerParam, propExpresion);

                    TryApplyLengthPrefix(expressions, propInfo, writerParam, instance);
                    expressions.AddRange(writeExpressions);
                }
            }
        }

        private static void AssertValidEnumerator(
            Type type,
            out MemberInfo currentMember,
            out MethodInfo moveNextMethod,
            out MethodInfo? disposeMethod)
        {
            const BindingFlags Binding =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            if (type == typeof(void))
                throw new ArgumentException("The enumerator may not be of type void.");

            {
                currentMember = type.GetMember("Current", Binding).FirstOrDefault() ??
                    type.GetInterfaces()
                    .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEnumerator<>))
                    ?.GetMember("Current").FirstOrDefault()!;

                if (currentMember == null)
                    throw new ArgumentException("The enumerator does not have a \"Current\" member.");
            }

            {
                moveNextMethod = type.GetMethod("MoveNext", Binding) ??
                    type.GetInterface(nameof(IEnumerator))?.GetMethod("MoveNext")!;

                if (moveNextMethod == null)
                    throw new ArgumentException("The enumerator does not have a \"MoveNext\" method.");

                if (moveNextMethod.ReturnType != typeof(bool))
                {
                    throw new ArgumentException(
                        "The enumerator's \"MoveNext\" method does not return a boolean}.");
                }
            }

            {
                disposeMethod = type.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public) ??
                    type.GetInterface(nameof(IDisposable))?.GetMethod("Dispose");
            }
        }

        private void ReflectiveWriteElement(
            DataSerializeMode mode,
            List<Expression> expressions,
            ParameterExpression writerParam,
            Expression instance)
        {
            if (mode == DataSerializeMode.Copy)
                throw new NotImplementedException(nameof(DataSerializeMode) + "." + mode.ToString());

            mode = PickModeByType(mode, instance.Type);

            void AddMethodCall(
                ParameterExpression? writerParam,
                MethodInfo writeMethod,
                Expression[] arguments)
            {
                expressions.Add(Expression.Call(writerParam, writeMethod, arguments));
            }

            if (instance.Type.IsEnum)
            {
                // Enums get sligthly special treatment.
                Type writeType = instance.Type.GetEnumUnderlyingType();

                var enumTypeKey = DataTypeKey.FromVoid(writeType);
                if (!DataTypeHandlers.TryGetValue(enumTypeKey, out var writeMethod))
                    throw new Exception("Missing enum write method for \"" + enumTypeKey + "\".");

                var args = new[] { Expression.Convert(instance, writeType) };
                AddMethodCall(writerParam, writeMethod, args);
            }
            else
            {
                Type? writeType = instance.Type;

                var writeInfos = new[]
                {
                    (Writer: writerParam,
                    Args: new Expression[] { instance },
                    Key: new DataTypeKey(typeof(void), writeType)),

                    (Writer: null,
                    Args: new Expression[] { writerParam, instance },
                    Key: new DataTypeKey(typeof(void), typeof(NetBinaryWriter), writeType))
                };

                foreach ((var Writer, var Args, var Key) in writeInfos)
                {
                    if (DataTypeHandlers.TryGetValue(Key, out var writeMethod))
                    {
                        AddMethodCall(Writer, writeMethod, Args);
                        return;
                    }
                }

                var dataObjectAttrib = writeType.GetCustomAttribute<DataObjectAttribute>();
                if (dataObjectAttrib != null)
                {
                    if (!DataObjectActions.TryGetValue(writeType, out var writeDelegate))
                    {
                        var writeBody = new List<Expression>();
                        var writeVariables = new List<ParameterExpression>();
                        var instanceParam = Expression.Parameter(writeType, "Instance");
                        ReflectiveWrite(writeBody, writerParam, instanceParam);

                        var writeBlock = Expression.Block(writeVariables, writeBody);
                        var writeLambda = Expression.Lambda(writeBlock, new[] { writerParam, instanceParam });

                        writeDelegate = writeLambda.Compile();
                        DataObjectActions.Add(writeType, writeDelegate);
                    }

                    expressions.Add(Expression.Invoke(Expression.Constant(writeDelegate), writerParam, instance));
                }
                else
                {
                    string msg = "Missing write method for \"" + writeType + "\".";

                    if (writeType.IsArray || writeType.GetMethod("GetEnumerator")?.ReturnType == typeof(bool))
                    {
                        msg += $"\nThe type can be enumerated. Consider using {nameof(DataEnumerableAttribute)}.";
                    }
                    throw new Exception(msg);
                }
            }
        }

        private void TryApplyLengthPrefix(
            List<Expression> expressions,
            DataPropertyInfo propInfo,
            ParameterExpression writerParam,
            Expression instance)
        {
            // TODO: respect LengthConstraint

            var lengthPrefixedAttrib = propInfo.Property.GetCustomAttribute<DataLengthPrefixedAttribute>();
            if (lengthPrefixedAttrib == null)
                return;

            switch (lengthPrefixedAttrib.LengthSource)
            {
                case LengthSource.ByName:
                {
                    var lengthMembers = instance.Type.GetMember("Length");
                    if (lengthMembers.Length == 0)
                        lengthMembers = instance.Type.GetMember("Count");

                    if (lengthMembers.Length == 0)
                    {
                        throw new Exception(
                            $"The length-prefixed property {instance} does not have a \"Length\" or \"Count\" member.");
                    }
                    var lengthMember =
                        lengthMembers.FirstOrDefault(x => x is PropertyInfo) ??
                        lengthMembers.FirstOrDefault(x => x is FieldInfo) ??
                        lengthMembers[0];

                    var length = Expression.MakeMemberAccess(instance, lengthMember);
                    var lengthWriteMethod = DataTypeHandlers[DataTypeKey.FromVoid(lengthPrefixedAttrib.LengthType)];
                    var propertyLength = Expression.Convert(length, lengthPrefixedAttrib.LengthType);
                    expressions.Add(Expression.Call(writerParam, lengthWriteMethod, new[] { propertyLength }));
                    break;
                }

                case LengthSource.Collection:
                {
                    var length = CollectionLength(instance);
                    var lengthWriteMethod = DataTypeHandlers[DataTypeKey.FromVoid(lengthPrefixedAttrib.LengthType)];
                    var propertyLength = Expression.Convert(length, lengthPrefixedAttrib.LengthType);
                    expressions.Add(Expression.Call(writerParam, lengthWriteMethod, new[] { propertyLength }));
                    break;
                }

                case LengthSource.WrittenBytes:
                    throw new NotImplementedException();

                default:
                    throw new InvalidOperationException(
                        "Unknown length source: " + lengthPrefixedAttrib.LengthSource);
            }
        }

        public static Expression CollectionLength(Expression instance)
        {
            if (instance.Type.GetGenericTypeDefinition() == typeof(ICollection<>))
                return Expression.Property(instance, typeof(ICollection<>).GetProperty("Count")!);

            if (instance.Type.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
                return Expression.Property(instance, typeof(IReadOnlyCollection<>).GetProperty("Count")!);

            throw new Exception(
                $"The expression is not of type {typeof(ICollection<>).Name} or {typeof(IReadOnlyCollection<>).Name}.");
        }

        private delegate void BlitSpanDelegateHelper(NetBinaryWriter writer, ReadOnlySpan<int> span);

        public static MethodInfo BlitSpanMethod { get; } =
            ((BlitSpanDelegateHelper)BlitSpan).Method.GetGenericMethodDefinition();

        public static void BlitSpan<T>(NetBinaryWriter writer, ReadOnlySpan<T> span)
            where T : unmanaged
        {
            if (typeof(T) == typeof(short))
            {
                throw new NotImplementedException();
                //var shorts = MemoryMarshal.Cast<T, short>(array.AsSpan());
                //writer.Write(shorts);
            }
            else if (typeof(T) == typeof(ushort))
            {
                throw new NotImplementedException();
                //var shorts = MemoryMarshal.Cast<T, ushort>(array.AsSpan());
                //writer.Write(shorts);
            }
            else if (typeof(T) == typeof(int))
            {
                var ints = MemoryMarshal.Cast<T, int>(span);
                writer.Write(ints);
            }
            else if (typeof(T) == typeof(uint))
            {
                throw new NotImplementedException();
                //var ints = MemoryMarshal.Cast<T, uint>(array.AsSpan());
                //writer.Write(ints);
            }
            else if (typeof(T) == typeof(long))
            {
                var longs = MemoryMarshal.Cast<T, long>(span);
                writer.Write(longs);
            }
            else if (typeof(T) == typeof(ulong))
            {
                var longs = MemoryMarshal.Cast<T, ulong>(span);
                writer.Write(longs);
            }
            else
            {
                var bytes = MemoryMarshal.AsBytes(span);
                writer.Write(bytes);
            }
        }
    }
}
