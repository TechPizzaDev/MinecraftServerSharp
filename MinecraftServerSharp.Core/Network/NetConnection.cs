using System;
using System.Net;
using System.Net.Sockets;
using MinecraftServerSharp.Network.Data;
using MinecraftServerSharp.Network.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Network
{
    public class NetConnection
    {
        private Action<NetConnection> _closeAction;
        private NetPacketDecoder _packetDecoder; // cached from NetManager
        private NetPacketEncoder _packetEncoder; // cached from NetManager

        public NetManager Manager { get; }
        public Socket Socket { get; }
        public SocketAsyncEventArgs ReceiveEvent { get; }
        public SocketAsyncEventArgs SendEvent { get; }
        public IPEndPoint RemoteEndPoint { get; }

        // TODO: make better use of the streams (recycle them better or something)
        public RecyclableMemoryStream ReceiveBuffer { get; }
        public RecyclableMemoryStream SendBuffer { get; }
        public NetBinaryReader Reader { get; }
        public NetBinaryWriter Writer { get; }

        public int ReceivedLength { get; set; } = -1;
        public int ReceivedLengthBytes { get; set; } = -1;
        public int TotalReceivedLength => ReceivedLength + ReceivedLengthBytes;

        public ProtocolState State { get; set; } = ProtocolState.Undefined;

        #region Constructors

        public NetConnection(
            NetManager manager,
            Socket socket,
            SocketAsyncEventArgs receiveEvent,
            SocketAsyncEventArgs sendEvent,
            Action<NetConnection> closeAction)
        {
            Manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            ReceiveEvent = receiveEvent ?? throw new ArgumentNullException(nameof(receiveEvent));
            SendEvent = sendEvent ?? throw new ArgumentNullException(nameof(sendEvent));
            _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

            ReceiveBuffer = RecyclableMemoryManager.Default.GetStream();
            SendBuffer = RecyclableMemoryManager.Default.GetStream();
            Reader = new NetBinaryReader(ReceiveBuffer);
            Writer = new NetBinaryWriter(SendBuffer);

            _packetDecoder = Manager.Processor.PacketDecoder;
            _packetEncoder = Manager.Processor.PacketEncoder;

            // get it here as we can't get it later if the socket gets disposed
            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        #endregion

        public int ReadPacket<TPacket>(out TPacket packet)
        {
            var reader = _packetDecoder.GetPacketReader<TPacket>();
            long oldPosition = Reader.Position;
            packet = reader.Invoke(Reader);
            return (int)(Reader.Position - oldPosition);
        }

        public TPacket ReadPacket<TPacket>()
        {
            ReadPacket(out TPacket packet);
            return packet;
        }

        public int WritePacket<TPacket>(TPacket packet)
        {
            var writer = _packetEncoder.GetPacketWriter<TPacket>();
            long oldPosition = Writer.Position;
            writer.Invoke(packet, Writer);
            return (int)(Writer.Position - oldPosition);
        }

        /// <summary>
        /// Removes the current message from the receive buffer
        /// while keeping all the to-be-processed data.
        /// </summary>
        public void TrimCurrentReceivedMessage()
        {
            int offset = TotalReceivedLength;
            TrimReceiveBuffer(offset);
        }

        public void TrimReceiveBuffer(int length)
        {
            ReceiveBuffer.TrimStart(length);
            ReceivedLength = -1;
            ReceivedLengthBytes = -1;
        }

        public void TrimSendBuffer(int length)
        {
            SendBuffer.TrimStart(length);
        }

        public bool Close()
        {
            if (_closeAction == null)
                return false;

            _closeAction.Invoke(this);
            _closeAction = null;
            State = ProtocolState.Disconnected;
            return true;
        }
    }
}
