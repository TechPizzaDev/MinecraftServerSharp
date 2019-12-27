using System.IO;

namespace MinecraftServerSharp.Network.Data
{
	// TODO: split NetBuffer into NetIncomingPacket and NetOutgoingPacket or something
	
	public partial class NetBuffer : INetBinaryReader, INetBinaryWriter, ISeekable
	{
		private MemoryStream _buffer;
		private NetBinaryReader _reader;
		private NetBinaryWriter _writer;

		public long Position => _buffer.Position;
		public long Length => _buffer.Length;

		public NetBuffer(MemoryStream backingBuffer)
		{
			_buffer = backingBuffer;
			_reader = new NetBinaryReader(_buffer);
			_writer = new NetBinaryWriter(_buffer);
		}

		public long Seek(int offset, SeekOrigin origin) => _buffer.Seek(offset, origin);
	}
}
