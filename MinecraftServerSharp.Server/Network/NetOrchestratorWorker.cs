using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using MinecraftServerSharp.Data.IO;
using MinecraftServerSharp.Net.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Net
{
    public partial class NetOrchestratorWorker : IDisposable
    {
        private delegate PacketWriteResult WritePacketDelegate(
            PacketHolder packetHolder,
            PacketSerializationMode mode,
            Stream destination);

        private static MethodInfo? WritePacketMethod { get; } =
            typeof(NetOrchestratorWorker).GetMethod(
                nameof(WritePacket), BindingFlags.Public | BindingFlags.Static);

        private static ConcurrentDictionary<Type, WritePacketDelegate> WritePacketDelegateCache { get; } =
            new ConcurrentDictionary<Type, WritePacketDelegate>();

        private AutoResetEvent _flushRequestEvent;
        private RecyclableMemoryStream _packetBuffer;

        public NetOrchestrator Orchestrator { get; }
        public Thread Thread { get; }

        public bool IsDisposed { get; private set; }
        public bool IsRunning { get; private set; }

        public NetOrchestratorWorker(NetOrchestrator orchestrator)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

            Thread = new Thread(ThreadRunner);
            _flushRequestEvent = new AutoResetEvent(false);
            _packetBuffer = Orchestrator.Processor.MemoryManager.GetStream();
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

                    while (Orchestrator.PacketSendQueue.TryDequeue(out var packetHolder))
                    {
                        Debug.Assert(packetHolder.TargetConnection != null, "Packet holder has no attached connection.");

                        if (packetHolder.TargetConnection.State != ProtocolState.Disconnected)
                        {
                            var writePacketDelegate = GetWritePacketDelegate(packetHolder.PacketType);

                            // TODO: compression
                            var result = writePacketDelegate.Invoke(
                                packetHolder, PacketSerializationMode.Uncompressed, _packetBuffer);

                            if (packetHolder.TargetConnection.State != ProtocolState.Disconnected)
                                processedConnections.Add(packetHolder.TargetConnection);
                        }
                        // TODO: return packet holder to the yet-to-exist pool
                    }

                    foreach (var connection in processedConnections)
                    {
                        lock (connection.WriteMutex)
                        {
                            Orchestrator.Processor.TryFlushSendBuffer(connection);
                        }

                        if (connection.State == ProtocolState.Closing)
                            connection.Close(immediate: true);
                    }
                    processedConnections.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception on thread \"{Thread.CurrentThread.Name}\": {ex}");
                }
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

            var connection = packetHolder.TargetConnection;
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
                if (!connection.Orchestrator.Processor.PacketEncoder.TryGetPacketIdDefinition(
                    holder.State, holder.PacketType, out var idDefinition))
                {
                    // We don't really want to continue if we don't even know what we're sending.
                    throw new Exception("Unknown packet ID.");
                }
                writer.WriteVar(idDefinition.RawId);
            }

            holder.WriterDelegate.Invoke(writer, holder.Packet);

            int dataLength = (int)writer.Length;
            int length = dataLength;
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
                connection.BufferWriter.WriteVar(dataLength);
                writer.BaseStream.SCopyTo(connection.SendBuffer);
            }

            return new PacketWriteResult(compressed, dataLength, length);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _flushRequestEvent.Dispose();
                    _packetBuffer.Dispose();
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
