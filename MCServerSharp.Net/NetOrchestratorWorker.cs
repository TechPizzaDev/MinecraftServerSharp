﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Data.IO;
using MCServerSharp.IO.Compression;
using MCServerSharp.Net.Packets;
using MCServerSharp.Utility;

namespace MCServerSharp.Net
{
    // TODO: allow using multiple/different codecs in one instance

    /// <summary>
    /// Controls a thread that decodes incoming and encodes outgoing messages.
    /// </summary>
    public partial class NetOrchestratorWorker : IDisposable
    {
        public delegate PacketWriteResult PacketWriteAction(
            PacketHolder packetHolder,
            Stream packetBuffer,
            Stream compressionBuffer);

        private static Action<Task<NetSendState>, object?> FinishSendQueueAction { get; } = FinishSendQueue;

        private static MethodInfo? WritePacketMethod { get; } =
            typeof(NetOrchestratorWorker).GetMethod(
                nameof(WritePacket), BindingFlags.Public | BindingFlags.Static);

        private static ConcurrentDictionary<Type, PacketWriteAction> GlobalPacketWriteActionCache { get; } =
            new ConcurrentDictionary<Type, PacketWriteAction>();

        // Having an action cache per worker should result in slightly lower overhead.
        private Dictionary<Type, PacketWriteAction> PacketWriteActionCache { get; } =
            new Dictionary<Type, PacketWriteAction>();

        private ChunkedMemoryStream _packetWriteBuffer;
        private ChunkedMemoryStream _packetCompressionBuffer;
        private ConcurrentQueue<NetPacketSendQueue> _queuesToFlush;
        private AutoResetEvent _flushRequestEvent;
        private int _busyFactor;

        public NetOrchestrator Orchestrator { get; }
        public Thread Thread { get; }

        public bool IsDisposed { get; private set; }
        public bool IsRunning { get; private set; }
        public int BusyFactor => _busyFactor;

        public NetOrchestratorWorker(NetOrchestrator orchestrator)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

            _packetWriteBuffer = Orchestrator.Codec.MemoryManager.GetStream();
            _packetCompressionBuffer = Orchestrator.Codec.MemoryManager.GetStream();
            _queuesToFlush = new();
            _flushRequestEvent = new(initialState: false);

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

        public static PacketWriteAction GetPacketWriteAction(Type packetType)
        {
            return GlobalPacketWriteActionCache.GetOrAdd(packetType, (type) =>
            {
                var genericMethod = WritePacketMethod!.MakeGenericMethod(type);
                return ReflectionHelper.CreateDelegateFromMethod<PacketWriteAction>(
                    genericMethod, useFirstArgumentAsInstance: false);
            });
        }

        private PacketWriteAction GetLocalPacketWriteAction(Type type)
        {
            if (!PacketWriteActionCache.TryGetValue(type, out var action))
            {
                action = GetPacketWriteAction(type);
                PacketWriteActionCache.Add(type, action);
            }
            return action;
        }

        public static PacketWriteResult WritePacket<TPacket>(
            PacketHolder packetHolder, Stream packetBuffer, Stream compressionBuffer)
        {
            if (packetHolder == null)
                throw new ArgumentNullException(nameof(packetHolder));
            if (packetBuffer == null)
                throw new ArgumentNullException(nameof(packetBuffer));
            if (compressionBuffer == null)
                throw new ArgumentNullException(nameof(compressionBuffer));

            var connection = packetHolder.Connection;
            if (connection == null)
                throw new Exception("Packet holder has no target connection.");

            var holder = (PacketHolder<TPacket>)packetHolder;

            // TODO: hold packets and data while closing, 
            //  in case of the client being able to somehow reconnect in the future

            if (!connection.Orchestrator.Codec.Encoder.TryGetPacketIdDefinition(
                holder.State, holder.PacketType, out var idDefinition))
            {
                // We don't really want to continue if we don't even know what we're sending.
                throw new NetUnknownPacketException(
                    "Failed to get server packet ID defintion.", holder.State, holder.PacketType);
            }

            var packetWriter = new NetBinaryWriter(packetBuffer)
            {
                Length = 0,
                Position = 0
            };
            packetWriter.WriteVar(idDefinition.RawId);
            holder.Writer.Invoke(packetWriter, holder.Packet);
            int dataLength = (int)packetWriter.Length;

            var resultWriter = new NetBinaryWriter(connection.SendBuffer);
            long initialResultPosition = resultWriter.Position;
            int? compressedLength = null;

            // CompressionThreshold < 0 == disabled
            // CompressionThreshold = 0 == enabled for all
            // CompressionThreshold > x == enabled for sizes >= x
            if (holder.CompressionThreshold >= 0)
            {
                bool compressed =
                    holder.CompressionThreshold == 0 ||
                    dataLength >= holder.CompressionThreshold;

                if (compressed)
                {
                    compressionBuffer.SetLength(0);
                    compressionBuffer.Position = 0;
                    using (var compressor = new ZlibStream(compressionBuffer, CompressionLevel.Fastest, true))
                    {
                        packetWriter.Position = 0;
                        packetWriter.BaseStream.SpanCopyTo(compressor);
                    }
                    compressedLength = (int)compressionBuffer.Length;

                    int packetLength = VarInt.GetEncodedSize(dataLength) + compressedLength.GetValueOrDefault();
                    resultWriter.WriteVar(packetLength);
                    resultWriter.WriteVar(dataLength);
                    compressionBuffer.Position = 0;
                    compressionBuffer.SpanCopyTo(resultWriter.BaseStream);
                }
                else
                {
                    int packetLength = VarInt.GetEncodedSize(0) + dataLength;
                    resultWriter.WriteVar(packetLength);
                    resultWriter.WriteVar(0);
                    packetWriter.Position = 0;
                    packetWriter.BaseStream.SpanCopyTo(resultWriter.BaseStream);
                }
            }
            else
            {
                int packetLength = dataLength;
                resultWriter.WriteVar(packetLength);
                packetWriter.Position = 0;
                packetWriter.BaseStream.SpanCopyTo(resultWriter.BaseStream);
            }

            long totalLength = resultWriter.Position - initialResultPosition;
            return new PacketWriteResult(dataLength, compressedLength, (int)totalLength);
        }

        private void ThreadRunner()
        {
            if (WritePacketMethod == null)
                throw new Exception($"{nameof(WritePacketMethod)} is null.");

            while (IsRunning)
            {
                if (!_queuesToFlush.TryDequeue(out NetPacketSendQueue? sendQueue))
                {
                    // Wait to not waste time on repeating loop.
                    _flushRequestEvent.WaitOne();
                    continue;
                }

                Interlocked.Decrement(ref _busyFactor);

                try
                {
                    try
                    {
                        while (sendQueue.TryPeek(out PacketHolder? peekedHolder))
                        {
                            try
                            {
                                bool sent = ProcessPacket(peekedHolder, out _);
                                if (sent)
                                {
                                    bool dequeued = sendQueue.TryDequeue(out PacketHolder? dequeuedHolder);
                                    Debug.Assert(dequeued);
                                    Debug.Assert(dequeuedHolder == peekedHolder);
                                }
                                else
                                {
                                    // TODO: somehow wait for a reconnect in some future implementation?
                                    break;
                                }
                            }
                            catch (NetUnknownPacketException netEx) when (
                                netEx.ProtocolState == ProtocolState.Closing ||
                                netEx.ProtocolState == ProtocolState.Disconnected)
                            {
                                sendQueue.Connection.Kick();
                                break;
                            }
                        }
                    }
                    finally
                    {
                        ValueTask<NetSendState> flushTask = sendQueue.Connection.FlushSendBuffer();
                        if (flushTask.IsCompleted)
                        {
                            FinishSendQueue(flushTask.Result, sendQueue);
                        }
                        else
                        {
                            flushTask.AsTask().ContinueWith(
                                FinishSendQueueAction, sendQueue, TaskContinuationOptions.ExecuteSynchronously);
                        }
                    }
                }
                catch (SocketException sockEx) when (sockEx.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // TODO: increment statistic?
                }
                catch (SocketException sockEx) when (sockEx.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    Console.WriteLine("Connection aborted for " + sendQueue.Connection.RemoteEndPoint);
                    // TODO: increment statistic?
                }
                catch (ObjectDisposedException) when (!sendQueue.Connection.Socket.Connected)
                {
                    // This should happen very rarely.

                    // TODO: increment statistic (tried to send with closed connection)?
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception on thread \"{Thread.CurrentThread.Name}\": {ex}");
                    sendQueue.Connection.Kick(ex);
                }
            }
        }

        private bool ProcessPacket(PacketHolder packetHolder, out PacketWriteResult writeResult)
        {
            Debug.Assert(
                packetHolder.Connection != null,
                "Packet holder has no attached connection.");

            try
            {
                if (!packetHolder.Connection.Socket.Connected)
                {
                    writeResult = default;
                    return false;
                }

                PacketWriteAction? packetWriteAction = GetLocalPacketWriteAction(packetHolder.PacketType);

                writeResult = packetWriteAction.Invoke(
                    packetHolder, _packetWriteBuffer, _packetCompressionBuffer);
            }
            finally
            {
                Orchestrator.ReturnPacketHolder(packetHolder);
            }
            return true;
        }

        private static void FinishSendQueue(Task<NetSendState> task, object? state)
        {
            var queue = (NetPacketSendQueue)state!;
            FinishSendQueue(task.Result, queue);
        }

        private static void FinishSendQueue(NetSendState state, NetPacketSendQueue queue)
        {
            lock (queue.EngageMutex)
            {
                queue.IsEngaged = false;

                if (!queue.IsEmpty)
                {
                    queue.Connection.Orchestrator.EnqueueQueue(queue);
                }
            }
        }

        public void Enqueue(NetPacketSendQueue queue)
        {
            _queuesToFlush.Enqueue(queue);

            Interlocked.Increment(ref _busyFactor);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _flushRequestEvent.Dispose();
                    _packetWriteBuffer.Dispose();
                    _packetCompressionBuffer.Dispose();
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
