using System;
using System.Net;
using System.Net.Sockets;
using MinecraftServerSharp.DataTypes;
using MinecraftServerSharp.Network.Data;
using MinecraftServerSharp.Network.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Network
{
    public class NetConnection
    {
        private Action<NetConnection> _closeAction;

        public NetProcessor Processor { get; }
        public Socket Socket { get; }
        public SocketAsyncEventArgs ReceiveEvent { get; }
        public SocketAsyncEventArgs SendEvent { get; }
        public IPEndPoint RemoteEndPoint { get; }

        // TODO: make better use of the streams (recycle them better or something)
        public RecyclableMemoryStream ReceiveBuffer { get; }
        public RecyclableMemoryStream SendBuffer { get; }
        public NetBinaryReader Reader { get; }
        public NetBinaryWriter Writer { get; }

        public ProtocolState State { get; set; }

        public int ReceivedLength { get; set; } = -1;
        public int ReceivedLengthBytes { get; set; } = -1;
        public int TotalReceivedLength => ReceivedLength + ReceivedLengthBytes;

        #region Constructors

        public NetConnection(
            NetProcessor processor,
            Socket socket,
            SocketAsyncEventArgs receiveEvent,
            SocketAsyncEventArgs sendEvent,
            Action<NetConnection> closeAction)
        {
            Processor = processor ?? throw new ArgumentNullException(nameof(processor));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            ReceiveEvent = receiveEvent ?? throw new ArgumentNullException(nameof(receiveEvent));
            SendEvent = sendEvent ?? throw new ArgumentNullException(nameof(sendEvent));
            _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

            ReceiveBuffer = Processor.MemoryManager.GetStream();
            SendBuffer = Processor.MemoryManager.GetStream();
            Reader = new NetBinaryReader(ReceiveBuffer);
            Writer = new NetBinaryWriter(SendBuffer);

            State = ProtocolState.Handshaking;

            // get it here as we can't get it later if the socket gets disposed
            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        #endregion

        public int ReadPacket<TPacket>(out TPacket packet)
        {
            var reader = Processor.PacketDecoder.GetPacketReader<TPacket>();
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
            var writer = Processor.PacketEncoder.GetPacketWriter<TPacket>();
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

        public void Kick(Exception exception)
        {
            KickCore("Server Error:\n" + exception);
        }

        public void Kick(string reason)
        {
            KickCore(reason);
        }

        /// <summary>
        /// TODO
        /// </summary>
        private void KickCore(string reason = null)
        {
            //var packet = new ServerDisconnect(new Chat(reason));
            //WritePacket(packet);

            Close();
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
