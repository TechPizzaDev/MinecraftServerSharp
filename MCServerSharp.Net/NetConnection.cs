﻿using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using MCServerSharp.Components;
using MCServerSharp.Data.IO;
using MCServerSharp.Net.Packets;
using MCServerSharp.Utility;

namespace MCServerSharp.Net
{
    public partial class NetConnection : ComponentEntity
    {
        private Action<NetConnection>? _closeAction;

        public NetOrchestrator Orchestrator { get; }
        public Socket Socket { get; }
        public IPEndPoint RemoteEndPoint { get; }

        // TODO: make better use of the streams (recycle them better or something)
        public ChunkedMemoryStream ReceiveBuffer { get; }
        public ChunkedMemoryStream DecompressionBuffer { get; }
        public ChunkedMemoryStream SendBuffer { get; }

        public object CloseMutex { get; } = new object();

        // TODO: add thread-safe property propagation
        public int CompressionThreshold { get; set; } = -1;
        public ProtocolState ProtocolState { get; set; }

        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }

        public bool IsAlive
        {
            get
            {
                return ProtocolState != ProtocolState.Closing
                    && ProtocolState != ProtocolState.Disconnected;
            }
        }

        #region Constructors

        public NetConnection(
            NetOrchestrator orchestrator,
            Socket socket,
            Action<NetConnection> closeAction)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

            // get it here as we can't get it later if the socket gets disposed
            RemoteEndPoint = (socket.RemoteEndPoint as IPEndPoint) ??
                throw new ArgumentException("The remote end point is null.", nameof(socket));

            ReceiveBuffer = Orchestrator.MemoryManager.GetStream();
            DecompressionBuffer = Orchestrator.MemoryManager.GetStream();
            SendBuffer = Orchestrator.MemoryManager.GetStream();

            ProtocolState = ProtocolState.Handshaking;
        }

        #endregion

        public OperationStatus ReadPacket<TPacket>(
            NetBinaryReader reader, out TPacket packet, out int length)
        {
            var readerAction = Orchestrator.Codec.Decoder.GetPacketReaderAction<TPacket>();

            long startPosition = reader.Position;
            var status = readerAction.Invoke(reader, out packet);

            length = (int)(reader.Position - startPosition);
            return status;
        }

        public void EnqueuePacket<TPacket>(TPacket packet)
        {
            Orchestrator.EnqueuePacket(this, packet);
        }

        public async ValueTask<NetSendState> FlushSendBuffer()
        {
            if (!Socket.Connected)
                return NetSendState.Closed;

            ChunkedMemoryStream sendBuffer = SendBuffer;
            int length = (int)sendBuffer.Length;
            if (length > 0)
            {
                int left = length;
                int block = 0;
                while (left > 0)
                {
                    Memory<byte> buffer = sendBuffer.GetBlock(block);
                    int blockLength = Math.Min(sendBuffer.BlockSize, left);

                    Memory<byte> data = buffer.Slice(0, blockLength);
                    int write = await Socket.SendAsync(data, SocketFlags.None).Unchain();
                    if (write == 0)
                    {
                        Close(immediate: false);
                        return NetSendState.Closing;
                    }

                    BytesSent += write;
                    left -= write;
                    block++;
                }

                SendBuffer.TrimStart(length);
            }
            return NetSendState.FullSend;
        }

        public void Kick(Exception? exception)
        {
#if DEBUG
            bool detailed = true;
#else
            bool detailed = false;
#endif

            Chat chat = default;
            if (exception != null)
            {
                string errorMessage =
                    (detailed ? exception.ToString() : exception.Message).Replace("\r", "");

                var dyn = new[]
                {
                    new { text = "Server Exception\n", bold = true },
                    new { text = errorMessage, bold = false }
                };
                chat = new Chat(new Utf8String(JsonSerializer.SerializeToUtf8Bytes(dyn)));
            }
            Kick(chat);
        }

        public void Kick(string? reason)
        {
            Chat chat = default;
            if (reason != null)
            {
                var dyn = new[]
                {
                    new { text = "Kicked by server\n", bold = true },
                    new { text = reason, bold = false }
                };
                chat = new Chat(new Utf8String(JsonSerializer.SerializeToUtf8Bytes(dyn)));
            }
            Kick(chat);
        }

        public void Kick(Chat reason = default)
        {
            if (ProtocolState == ProtocolState.Play)
            {
                var packet = new ServerPlayDisconnect(reason);
                EnqueuePacket(packet);
            }
            else if (
                ProtocolState == ProtocolState.Login ||
                ProtocolState == ProtocolState.Status)
            {
                var packet = new ServerLoginDisconnect(reason);
                EnqueuePacket(packet);
            }

            Close(immediate: false);
        }

        public void Close(bool immediate)
        {
            if (!immediate)
            {
                ProtocolState = ProtocolState.Closing;
                return;
            }

            lock (CloseMutex)
            {
                if (_closeAction == null)
                    return;

                ProtocolState = ProtocolState.Disconnected;
                _closeAction.Invoke(this);
                _closeAction = null;

                // TODO: finalize metrics and use them somehow
                Console.WriteLine("Connection metrics; Sent: " + BytesSent + ", Received: " + BytesReceived);
            }
        }
    }
}
