using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using MCServerSharp.Data.IO;
using MCServerSharp.Net.Packets;
using MCServerSharp.Utility;

namespace MCServerSharp.Net
{
    public partial class NetConnection
    {
        private Action<NetConnection>? _closeAction;

        public NetOrchestrator Orchestrator { get; }
        public Socket Socket { get; }
        public IPEndPoint RemoteEndPoint { get; }

        // TODO: make better use of the streams (recycle them better or something)
        public ChunkedMemoryStream ReceiveBuffer { get; }
        public ChunkedMemoryStream SendBuffer { get; }

        public object CloseMutex { get; } = new object();

        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }

        // TODO: add thread-safe protocol state propagation
        public ProtocolState ProtocolState { get; set; }

        public string? UserName { get; set; }

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
            SendBuffer = Orchestrator.MemoryManager.GetStream();

            ProtocolState = ProtocolState.Handshaking;
        }

        #endregion

        public OperationStatus ReadPacket<TPacket>(out TPacket packet, out int length)
        {
            var packetReader = Orchestrator.Codec.Decoder.GetPacketReader<TPacket>();
            var reader = new NetBinaryReader(ReceiveBuffer);

            long startPosition = reader.Position;
            var status = packetReader.Invoke(reader, out packet);

            length = (int)(reader.Position - startPosition);
            return status;
        }

        public void EnqueuePacket<TPacket>(TPacket packet)
        {
            Orchestrator.EnqueuePacket(this, packet);
        }

        public void Kick(Exception? exception)
        {
            bool detailed = false;
            
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
                else if (ProtocolState == ProtocolState.Login)
                {
                    var packet = new ServerLoginDisconnect(reason.Value);
                    EnqueuePacket(packet);
                }
                Orchestrator.RequestFlush();
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

                //Console.WriteLine("Connection metrics; Sent: " + BytesSent + ", Received: " + BytesReceived);
            }
        }
    }
}
