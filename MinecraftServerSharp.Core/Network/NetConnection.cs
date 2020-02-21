using System;
using System.Net;
using System.Net.Sockets;
using MinecraftServerSharp.Network.Data;
using MinecraftServerSharp.Network.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Network
{
    public partial class NetConnection
    {
        private Action<NetConnection> _closeAction;

        public NetOrchestrator Orchestrator { get; }
        public Socket Socket { get; }
        public SocketAsyncEventArgs ReceiveEvent { get; }
        public SocketAsyncEventArgs SendEvent { get; }
        public IPEndPoint RemoteEndPoint { get; }

        // TODO: make better use of the streams (recycle them better or something)
        public RecyclableMemoryStream ReceiveBuffer { get; }
        public RecyclableMemoryStream SendBuffer { get; }
        public NetBinaryReader Reader { get; }
        public NetBinaryWriter Writer { get; }

        public object WriteMutex { get; } = new object();
        public object SendMutex { get; } = new object();

        public int ReceivedLength { get; set; } = -1;
        public int ReceivedLengthBytes { get; set; } = -1;
        public int TotalReceivedLength => ReceivedLength + ReceivedLengthBytes;

        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }

        public ProtocolState State { get; set; }

        #region Constructors

        public NetConnection(
            NetOrchestrator orchestrator,
            Socket socket,
            SocketAsyncEventArgs receiveEvent,
            SocketAsyncEventArgs sendEvent,
            Action<NetConnection> closeAction)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            ReceiveEvent = receiveEvent ?? throw new ArgumentNullException(nameof(receiveEvent));
            SendEvent = sendEvent ?? throw new ArgumentNullException(nameof(sendEvent));
            _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

            ReceiveBuffer = Orchestrator.Processor.MemoryManager.GetStream();
            SendBuffer = Orchestrator.Processor.MemoryManager.GetStream();
            Reader = new NetBinaryReader(ReceiveBuffer);
            Writer = new NetBinaryWriter(SendBuffer);

            // get it here as we can't get it later if the socket gets disposed
            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;

            State = ProtocolState.Handshaking;
        }

        #endregion

        public (ReadCode Code, int Length) ReadPacket<TPacket>(out TPacket packet)
        {
            var reader = Orchestrator.Processor.PacketDecoder.GetPacketReader<TPacket>();
            long oldPosition = Reader.Position;
            var resultCode = reader.Invoke(Reader, out packet);
            int length = (int)(Reader.Position - oldPosition);
            return (resultCode, length);
        }
        
        public PacketHolder<TPacket> EnqueuePacket<TPacket>(TPacket packet)
        {
            return Orchestrator.EnqueuePacket(this, packet);
        }

        public void TrimReceiveBufferStart(int length)
        {
            ReceiveBuffer.TrimStart(length);

            ReceivedLength = -1;
            ReceivedLengthBytes = -1;
        }

        public void TrimSendBufferStart(int length)
        {
            SendBuffer.TrimStart(length);
        }

        /// <summary>
        /// Removes the first message from the receive buffer
        /// while keeping all the to-be-processed data.
        /// </summary>
        public void TrimFirstReceivedMessage()
        {
            int offset = TotalReceivedLength;
            TrimReceiveBufferStart(offset);
        }

        public void Kick(Exception exception)
        {
            KickCore("Server Error:\n" + exception);
        }

        public void Kick(string reason)
        {
            KickCore(reason);
        }

        private void KickCore(string reason = null)
        {
            // TODO: Send the reason as a message

            //var packet = new ServerDisconnect(new Chat(reason));
            //WritePacket(packet);

            Close();
        }

        public bool Close()
        {
            lock (SendMutex)
            {
                if (_closeAction == null)
                    return false;

                State = ProtocolState.Disconnected;
                _closeAction.Invoke(this);
                _closeAction = null;

                Console.WriteLine("Connection metrics; Sent: " + BytesSent + ", Received: " + BytesReceived);

                return true;
            }
        }
    }
}
