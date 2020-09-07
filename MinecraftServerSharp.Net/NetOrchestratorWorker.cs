using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
        public delegate PacketWriteResult WritePacketDelegate(
            PacketHolder packetHolder,
            PacketSerializationMode mode,
            Stream destination);

        private static MethodInfo? WritePacketMethod { get; } =
            typeof(NetOrchestratorWorker).GetMethod(
                nameof(WritePacket), BindingFlags.Public | BindingFlags.Static);

        private static ConcurrentDictionary<Type, WritePacketDelegate> WritePacketDelegateCache { get; } =
            new ConcurrentDictionary<Type, WritePacketDelegate>();

        private ChunkedMemoryStream _packetWriteBuffer;
        private AutoResetEvent _flushRequestEvent;

        public NetOrchestrator Orchestrator { get; }
        public Thread Thread { get; }

        public bool IsDisposed { get; private set; }
        public bool IsRunning { get; private set; }

        public NetOrchestratorWorker(NetOrchestrator orchestrator)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

            _packetWriteBuffer = Orchestrator.Codec.MemoryManager.GetStream();
            _flushRequestEvent = new AutoResetEvent(false);

            Thread = new Thread(ThreadRunner);
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

        public static WritePacketDelegate GetWritePacketDelegate(Type packetType)
        {
            return WritePacketDelegateCache.GetOrAdd(packetType, (type) =>
            {
                var genericMethod = WritePacketMethod!.MakeGenericMethod(type);
                return ReflectionHelper.CreateDelegateFromMethod<WritePacketDelegate>(
                    genericMethod, useFirstArgumentAsInstance: false);
            });
        }

        public static PacketWriteResult WritePacket<TPacket>(
            PacketHolder packetHolder, PacketSerializationMode mode, Stream bufferStream)
        {
            if (packetHolder == null)
                throw new ArgumentNullException(nameof(packetHolder));

            var connection = packetHolder.Connection;
            if (connection == null)
                throw new Exception("Packet holder has no target connection.");

            var holder = (PacketHolder<TPacket>)packetHolder;
            var bufferWriter = new NetBinaryWriter(bufferStream)
            {
                Position = 0,
                Length = 0
            };

            if (mode != PacketSerializationMode.NoHeader)
            {
                if (!connection.Orchestrator.Codec.Encoder.TryGetPacketIdDefinition(
                    holder.State, holder.PacketType, out var idDefinition))
                {
                    Console.WriteLine("why: " + holder.State + ": " + idDefinition.Id);

                    // We don't really want to continue if we don't even know what we're sending.
                    throw new Exception(
                        $"Failed to get server packet ID defintion " +
                        $"(State: {holder.State}, Type: {holder.PacketType}).");
                }
                bufferWriter.WriteVar(idDefinition.RawId);
            }

            holder.Writer.Invoke(bufferWriter, holder.Packet);

            int rawLength = (int)bufferWriter.Length;
            int length = rawLength;
            bool compressed = false;

            if (mode == PacketSerializationMode.Compressed)
            {
                throw new NotImplementedException();
                // TODO: compress packet buffer and reassign "length" variable
                compressed = true;
            }

            var resultWriter = new NetBinaryWriter(connection.SendBuffer);
            resultWriter.WriteVar(rawLength);

            bufferWriter.Position = 0;
            bufferWriter.BaseStream.SCopyTo(resultWriter.BaseStream);

            return new PacketWriteResult(compressed, rawLength, length);
        }

        private void ThreadRunner()
        {
            if (WritePacketMethod == null)
                throw new Exception($"{nameof(WritePacketMethod)} is null.");

            int timeoutMillis = 100;

            while (IsRunning)
            {
                try
                {
                    // Wait to not waste time on repeating loop.
                    _flushRequestEvent.WaitOne(timeoutMillis);

                    if (!Orchestrator.QueuesToFlush.TryDequeue(out var orchestratorQueue))
                        continue;

                    var connection = orchestratorQueue.Connection;

                    while (orchestratorQueue.SendQueue.TryDequeue(out var packetHolder))
                    {
                        Debug.Assert(
                            packetHolder.Connection != null, "Packet holder has no attached connection.");

                        if (packetHolder.Connection.ProtocolState != ProtocolState.Disconnected)
                        {
                            var structAttrib = packetHolder.PacketType.GetCustomAttribute<PacketStructAttribute>();

                            var mode = PacketSerializationMode.Uncompressed;

                            var writePacketDelegate = GetWritePacketDelegate(packetHolder.PacketType);

                            // TODO: compression
                            var result = writePacketDelegate.Invoke(packetHolder, mode, _packetWriteBuffer);
                        }

                        // TODO: batch return of holders for less locking
                        Orchestrator.ReturnPacketHolder(packetHolder);
                    }

                    Task.Run(async () => await Orchestrator.Codec.FlushSendBuffer(connection)).ContinueWith((task) =>
                    {
                        lock (Orchestrator.OccupiedQueues)
                            Orchestrator.OccupiedQueues.Remove(orchestratorQueue);
                    
                    }, TaskContinuationOptions.ExecuteSynchronously);

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
