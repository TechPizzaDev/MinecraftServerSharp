using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using MinecraftServerSharp.Network.Data;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Network
{
    public partial class NetOrchestratorWorker
    {
        private AutoResetEvent _flushRequestEvent;
        private AutoResetEvent _flushFinishEvent;
        private RecyclableMemoryStream _packetBuffer;

        public NetOrchestrator Orchestrator { get; }
        public Thread Thread { get; }

        public bool IsRunning { get; private set; }

        public NetOrchestratorWorker(NetOrchestrator orchestrator)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

            Thread = new Thread(ThreadRunner);
            _flushRequestEvent = new AutoResetEvent(false);
            _flushFinishEvent = new AutoResetEvent(false);
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

        public void Flush()
        {
            _flushRequestEvent.Set();
        }

        public void AwaitFlush()
        {
            _flushFinishEvent.WaitOne();
        }

        private void ThreadRunner()
        {
            var activeConnections = new HashSet<NetConnection>();

            while (IsRunning)
            {
                if (!_flushRequestEvent.WaitOne(TimeSpan.FromMilliseconds(100)))
                    continue;

                while (Orchestrator.PacketSendQueue.TryDequeue(out var packetHolder))
                {
                    if (packetHolder.TargetConnection.State != Packets.ProtocolState.Disconnected)
                    {
                        var writePacketMethod = typeof(NetOrchestratorWorker).GetMethod(
                            nameof(WritePacket), BindingFlags.NonPublic | BindingFlags.Instance);

                        // TODO: cache/expression lambda this stuff
                        var method = writePacketMethod.MakeGenericMethod(packetHolder.PacketType);
                        var result = (PacketWriteResult)method.Invoke(
                            this, new object[] { packetHolder, PacketSerializationMode.Uncompressed });

                        if (packetHolder.TargetConnection.State != Packets.ProtocolState.Disconnected &&
                            !result.Success)
                            throw new Exception("Failed to write packet.");

                        activeConnections.Add(packetHolder.TargetConnection);
                    }
                    // TODO: return packet holder to the yet-to-be pool
                }

                foreach (var connection in activeConnections)
                    Orchestrator.Processor.FlushSendBuffer(connection);
                activeConnections.Clear();

                _flushFinishEvent.Set();
            }
        }

        private PacketWriteResult WritePacket<TPacket>(
            PacketHolder<TPacket> packetHolder, PacketSerializationMode mode)
        {
            var connection = packetHolder.TargetConnection;
            var writer = new NetBinaryWriter(_packetBuffer);

            if (mode == PacketSerializationMode.Uncompressed ||
                mode == PacketSerializationMode.Compressed)
            {
                if (!Orchestrator.Processor.PacketEncoder.TryGetPacketIdDefinition(
                    connection.State, packetHolder.PacketType, out var idDefinition))
                {
                    // We don't really want to continue if we don't even know what we're sending.
                    return PacketWriteResult.Failed;
                }
                writer.Write((VarInt)idDefinition.RawID);
            }

            packetHolder.WriterDelegate.Invoke(writer, packetHolder.Packet);

            int dataLength = (int)_packetBuffer.Length;
            int length = dataLength;
            bool compressed = false;

            _packetBuffer.Position = 0;
            lock (connection.WriteMutex)
            {
                if (mode == PacketSerializationMode.Compressed)
                {
                    throw new NotImplementedException();
                    // TODO: compress packet buffer and reassign "length" variable
                    compressed = true;
                }

                connection.Writer.Write((VarInt)dataLength);
                _packetBuffer.SCopyTo(connection.SendBuffer);
            }
            _packetBuffer.SetLength(0);

            return new PacketWriteResult(success: true, compressed, dataLength, length);
        }
    }
}
