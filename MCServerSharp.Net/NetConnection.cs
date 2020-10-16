using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using MCServerSharp.Data.IO;
using MCServerSharp.Maths;
using MCServerSharp.Net.Packets;
using MCServerSharp.Utility;

namespace MCServerSharp.Net
{
    public struct ChunkPosition : IEquatable<ChunkPosition>
    {
        public int X;
        public int Z;

        public ChunkPosition(int x, int y)
        {
            X = x;
            Z = y;
        }

        public readonly bool Equals(ChunkPosition other)
        {
            return X == other.X
                && Z == other.Z;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is ChunkPosition value && Equals(value);
        }

        public override readonly string ToString()
        {
            return "{X:" + X + ", Z:" + Z + "}";
        }

        public static bool operator ==(ChunkPosition left, ChunkPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkPosition left, ChunkPosition right)
        {
            return !(left == right);
        }
    }

    public partial class NetConnection
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

        // TODO: move this to Player class
        public string? UserName { get; set; }

        public ChunkPosition ChunkPosition { get; set; }
        public ChunkPosition LastChunkPosition { get; set; }

        public Vector3d PlayerPosition { get; set; }
        public Vector3d LastPosition { get; set; }

        public ClientSettings ClientSettings { get; set; }
        public bool ClientSettingsChanged { get; set; }

        public HashSet<(int, int)> SentChunks = new HashSet<(int, int)>();

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
            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;

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

            var sendBuffer = SendBuffer;
            int length = (int)sendBuffer.Length;
            if (length > 0)
            {
                int left = length;
                int block = 0;
                while (left > 0)
                {
                    var buffer = sendBuffer.GetBlock(block);
                    int blockLength = Math.Min(sendBuffer.BlockSize, left);

                    var data = buffer.Slice(0, blockLength);
                    int write = await Socket.SendAsync(data, SocketFlags.None).ConfigureAwait(false);
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

            Chat? chat = null;
            if (exception != null)
            {
                string errorMessage =
                    (detailed ? exception.ToString() : exception.Message).Replace("\r", "");

                var dyn = new[]
                {
                    new { text = "Server Exception\n", bold = true },
                    new { text = errorMessage, bold = false }
                };
                chat = new Chat((Utf8String)JsonSerializer.Serialize(dyn));
            }
            Kick(chat);
        }

        public void Kick(string? reason = null)
        {
            Chat? chat = null;
            if (reason != null)
            {
                var dyn = new[]
                {
                    new { text = "Kicked by server\n", bold = true },
                    new { text = reason, bold = false }
                };
                chat = new Chat((Utf8String)JsonSerializer.Serialize(dyn));
            }
            Kick(chat);
        }

        public void Kick(Chat? reason = null)
        {
            if (reason != null)
            {
                if (ProtocolState == ProtocolState.Play)
                {
                    var packet = new ServerPlayDisconnect(reason.Value);
                    EnqueuePacket(packet);
                }
                else if (
                    ProtocolState == ProtocolState.Login ||
                    ProtocolState == ProtocolState.Status)
                {
                    var packet = new ServerLoginDisconnect(reason.Value);
                    EnqueuePacket(packet);
                }
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
