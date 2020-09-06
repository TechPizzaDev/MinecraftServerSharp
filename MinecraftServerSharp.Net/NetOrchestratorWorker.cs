using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using MinecraftServerSharp.Data.IO;
using MinecraftServerSharp.Net.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Net
{
    // TODO: allow using multiple/different codecs in one instance

    /// <summary>
    /// Controls a thread that decodes incoming and encodes outgoing messages.
    /// </summary>
    public partial class NetOrchestratorWorker : IDisposable
    {
        public const int PacketPoolItemLimit = 64;
        public const int PacketPoolCommonItemLimit = 256;

        private delegate PacketWriteResult WritePacketDelegate(
            PacketHolder packetHolder,
            PacketSerializationMode mode,
            Stream destination);

        private static MethodInfo? WritePacketMethod { get; } =
            typeof(NetOrchestratorWorker).GetMethod(
                nameof(WritePacket), BindingFlags.Public | BindingFlags.Static);

        private static ConcurrentDictionary<Type, WritePacketDelegate> WritePacketDelegateCache { get; } =
            new ConcurrentDictionary<Type, WritePacketDelegate>();

        private static HashSet<Type> _commonPacketTypes = new HashSet<Type>
        {
            typeof(ServerKeepAlive),
            typeof(ClientKeepAlive),
            typeof(ClientPlayerPosition),
            typeof(ClientPlayerRotation),
            typeof(ClientPlayerPositionRotation)
        };

        private ChunkedMemoryStream _packetWriteBuffer;
        private PacketHolderPool _packetHolderPool;
        private AutoResetEvent _flushRequestEvent;
        private Queue<PacketHolder> _packetSendQueue;

        public NetOrchestrator Orchestrator { get; }
        public Thread Thread { get; }

        public bool IsDisposed { get; private set; }
        public bool IsRunning { get; private set; }

        public int PacketSendQueueCount => _packetSendQueue.Count;

        public NetOrchestratorWorker(NetOrchestrator orchestrator)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

            _packetWriteBuffer = Orchestrator.Codec.MemoryManager.GetStream();
            _packetHolderPool = new PacketHolderPool(StorePacketPredicate);
            _flushRequestEvent = new AutoResetEvent(false);
            _packetSendQueue = new Queue<PacketHolder>();

            Thread = new Thread(ThreadRunner);
        }

        private static bool StorePacketPredicate(
            PacketHolderPool sender, Type packetType, int currentCount)
        {
            int limit = PacketPoolItemLimit;

            if (_commonPacketTypes.Contains(packetType))
                limit = PacketPoolCommonItemLimit;

            return currentCount < limit;
        }

        public void Start()
        {
            IsRunning = true;
            Thread.Start();
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void RequestFlush()
        {
            _flushRequestEvent.Set();
        }

        public PacketHolder<TPacket> GetPacketHolder<TPacket>(TPacket packet, NetConnection connection)
        {
            var encoder = Orchestrator.Codec.Encoder;
            var writer = encoder.GetPacketWriter<TPacket>();

            lock (_packetHolderPool)
            {
                return _packetHolderPool.Rent(
                    writer,
                    connection,
                    packet);
            }
        }

        private static WritePacketDelegate GetWritePacketDelegate(Type packetType)
        {
            return WritePacketDelegateCache.GetOrAdd(packetType, (type) =>
            {
                var genericMethod = WritePacketMethod!.MakeGenericMethod(type);
                return ReflectionHelper.CreateDelegateFromMethod<WritePacketDelegate>(
                    genericMethod, useFirstArgumentAsInstance: false);
            });
        }

        public static PacketWriteResult WritePacket<TPacket>(
            PacketHolder packetHolder, PacketSerializationMode mode, Stream destination)
        {
            if (packetHolder == null)
                throw new ArgumentNullException(nameof(packetHolder));

            var connection = packetHolder.Connection;
            if (connection == null)
                throw new Exception("Packet holder has no target connection.");

            var holder = (PacketHolder<TPacket>)packetHolder;
            var writer = new NetBinaryWriter(destination)
            {
                Position = 0,
                Length = 0
            };

            if (mode != PacketSerializationMode.NoHeader)
            {
                if (!connection.Orchestrator.Codec.Encoder.TryGetPacketIdDefinition(
                    holder.State, holder.PacketType, out var idDefinition))
                {
                    // We don't really want to continue if we don't even know what we're sending.
                    throw new Exception("Undefined server packet ID.");
                }
                writer.WriteVar(idDefinition.RawId);
            }

            holder.Writer.Invoke(writer, holder.Packet);

            int rawLength = (int)writer.Length;
            int length = rawLength;
            bool compressed = false;

            if (mode == PacketSerializationMode.Compressed)
            {
                throw new NotImplementedException();
                // TODO: compress packet buffer and reassign "length" variable
                compressed = true;
            }

            writer.Position = 0;
            lock (connection.WriteMutex)
            {
                connection.BufferWriter.WriteVar(rawLength);
                writer.BaseStream.SCopyTo(connection.SendBuffer);
            }

            return new PacketWriteResult(compressed, rawLength, length);
        }

        public void EnqueuePacket(PacketHolder packetHolder)
        {
            lock (_packetSendQueue)
                _packetSendQueue.Enqueue(packetHolder);
        }

        private bool TryDequeuePacket([MaybeNullWhen(false)] out PacketHolder packetHolder)
        {
            lock (_packetSendQueue)
                return _packetSendQueue.TryDequeue(out packetHolder);
        }

        private void ThreadRunner()
        {
            if (WritePacketMethod == null)
                throw new Exception($"{nameof(WritePacketMethod)} is null.");

            var processedConnections = new HashSet<NetConnection>();
            int timeoutMillis = 50;

            while (IsRunning)
            {
                try
                {
                    // Wait to not waste time on repeating loop.
                    _flushRequestEvent.WaitOne(timeoutMillis);

                    while (TryDequeuePacket(out var packetHolder))
                    {
                        Debug.Assert(
                            packetHolder.Connection != null, "Packet holder has no attached connection.");

                        if (packetHolder.Connection.State != ProtocolState.Disconnected)
                        {
                            var writePacketDelegate = GetWritePacketDelegate(packetHolder.PacketType);

                            // TODO: compression
                            var result = writePacketDelegate.Invoke(
                                packetHolder, PacketSerializationMode.Uncompressed, _packetWriteBuffer);

                            if (packetHolder.Connection.State != ProtocolState.Disconnected)
                                processedConnections.Add(packetHolder.Connection);
                        }

                        lock (_packetHolderPool)
                            _packetHolderPool.Return(packetHolder);
                    }

                    foreach (var connection in processedConnections)
                    {
                        try
                        {
                            lock (connection.WriteMutex)
                            {
                                Orchestrator.Codec.TryFlushSendBuffer(connection);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception while flushing send buffer: {ex}");
                        }
                        finally
                        {
                            if (connection.State == ProtocolState.Closing)
                                connection.Close(immediate: true);
                        }
                    }
                    processedConnections.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception on thread \"{Thread.CurrentThread.Name}\": {ex}");
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _flushRequestEvent.Dispose();
                    _packetWriteBuffer.Dispose();
                }

                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
