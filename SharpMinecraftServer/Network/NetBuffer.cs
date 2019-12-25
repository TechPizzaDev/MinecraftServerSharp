using System.IO;

namespace SharpMinecraftServer.Network
{
	// TODO: pool netbuffers

	public partial class NetBuffer
	{
		private MemoryStream _buffer;
		private NetBinaryReader _reader;
		private NetBinaryWriter _writer;

		public NetBuffer(MemoryStream backingBuffer)
		{
			_buffer = backingBuffer;
			_reader = new NetBinaryReader(_buffer);
			_writer = new NetBinaryWriter(_buffer);
		}

		public long Seek(int offset, SeekOrigin origin) => _buffer.Seek(offset, origin);
	}
}
