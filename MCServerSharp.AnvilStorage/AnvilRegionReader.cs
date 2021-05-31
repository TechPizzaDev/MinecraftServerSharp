using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp;
using MCServerSharp.Data.IO;
using MCServerSharp.IO.Compression;
using MCServerSharp.Maths;
using MCServerSharp.NBT;
using MCServerSharp.Utility;

namespace MCServerSharp.AnvilStorage
{
    public class AnvilRegionReader
    {
        private Task? _fullLoadTask;

        private object LoadMutex { get; } = new();
        private Dictionary<ChunkColumnPosition, (int Index, NbtDocument Data)> _documentSet = new();
        private List<NbtDocument?> _documentList = new();

        public ChunkLocation[] Locations = new ChunkLocation[1024];
        public int[] Timestamps = new int[1024];
        public int[] LocationIndices = new int[1024];
        public int FirstValidLocation;

        public NetBinaryReader Stream { get; }
        public ArrayPool<byte> Pool { get; }

        public int ChunkCount => 1024 - FirstValidLocation;

        public AnvilRegionReader(NetBinaryReader stream, ArrayPool<byte> pool)
        {
            if (stream.BaseStream == null)
                throw new ArgumentNullException(nameof(stream));
            Stream = stream;
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        public static OperationStatus Create(
            NetBinaryReader stream, ArrayPool<byte> pool, out AnvilRegionReader regionReader)
        {
            regionReader = new AnvilRegionReader(stream, pool);

            ChunkLocation[] locations = regionReader.Locations;
            var locationsStatus = stream.Read(MemoryMarshal.AsBytes(locations.AsSpan()));
            if (locationsStatus != OperationStatus.Done)
                return locationsStatus;

            var timestampsStatus = stream.Read(regionReader.Timestamps);
            if (timestampsStatus != OperationStatus.Done)
                return timestampsStatus;

            int[] locationIndices = regionReader.LocationIndices;
            for (int i = 0; i < locationIndices.Length; i++)
                locationIndices[i] = i;

            Array.Sort(locations, locationIndices);

            // Find the first valid chunk location.
            for (int i = 0; i < locations.Length; i++)
            {
                if (locations[i].SectorCount != 0)
                {
                    regionReader.FirstValidLocation = i;
                    break;
                }
            }

            return OperationStatus.Done;
        }

        private async Task LoadFullAsync(CancellationToken cancellationToken)
        {
            using var compressedData = RecyclableMemoryManager.Default.GetStream();
            using var decompressedData = RecyclableMemoryManager.Default.GetStream();

            for (int i = 0; i < ChunkCount; i++)
            {
                int locationIndex = FirstValidLocation + i;
                ChunkLocation location = Locations[locationIndex];

                // Some files have empty sectors between chunks so check if skip is needed.
                long sectorPosition = location.SectorOffset * 4096;
                if (Stream.Position != sectorPosition) // this should always be a multiple of 4096
                {
                    long toSkip = sectorPosition - Stream.Position;
                    if (toSkip < 0) // locations are ordered by offset so this should never throw
                        throw new InvalidDataException();

                    Stream.Seek(toSkip, SeekOrigin.Current);
                }

                var lengthStatus = Stream.Read(out int actualDataLength);
                if (lengthStatus != OperationStatus.Done)
                    throw new EndOfStreamException();

                var compressionTypeStatus = Stream.Read(out byte compressionType);
                if (compressionTypeStatus != OperationStatus.Done)
                    throw new EndOfStreamException();

                // Copy all remaining bytes to reduce future seeking.
                int remainingByteCount = location.SectorCount * 4096 - sizeof(int) - sizeof(byte);

                compressedData.Position = 0;
                var compressedDataStatus = await Stream.WriteToAsync(
                    compressedData, remainingByteCount, cancellationToken).Unchain();
                if (compressedDataStatus != OperationStatus.Done)
                    throw new EndOfStreamException();

                // Now actually use what we was specified and not all we read.
                compressedData.SetLength(actualDataLength - 1);
                compressedData.Position = 0;

                decompressedData.SetLength(0);
                Stream dataStream;
                switch (compressionType)
                {
                    case 1:
                        using (var decompStream = new GZipStream(compressedData, CompressionMode.Decompress, leaveOpen: true))
                        {
                            await decompStream.CopyToAsync(decompressedData, cancellationToken);
                        }
                        dataStream = decompressedData;
                        break;

                    case 2:
                        using (var decompStream = new ZlibStream(compressedData, CompressionMode.Decompress, leaveOpen: true))
                        {
                            await decompStream.CopyToAsync(decompressedData, cancellationToken);
                        }
                        dataStream = decompressedData;
                        break;

                    case 3:
                        dataStream = compressedData;
                        break;

                    default:
                        throw new InvalidDataException("Unknown compression type.");
                };
                decompressedData.Position = 0;

                // Seekable streams allow Parse to know how much it needs to allocate.
                NbtDocument chunkDocument = NbtDocument.Parse(
                    dataStream,
                    NbtOptions.JavaDefault,
                    null); // TODO: improve memory management

                //int chunkIndex = LocationIndices[locationIndex];

                // TODO: add some kind of NbtDocument-to-(generic)object helper and NbtSerializer

                ChunkColumnPosition columnPosition = GetColumnPosition(chunkDocument.RootTag);

                _documentSet.Add(columnPosition, (i, chunkDocument));
                _documentList.Add(chunkDocument);
            }
        }

        public static ChunkColumnPosition GetColumnPosition(NbtElement chunkRoot)
        {
            NbtElement levelCompound = chunkRoot["Level"];
            int columnX = levelCompound["xPos"].GetInt();
            int columnZ = levelCompound["zPos"].GetInt();
            return new ChunkColumnPosition(columnX, columnZ);
        }

        public async ValueTask CompleteFullLoad(CancellationToken cancellationToken)
        {
            if (_fullLoadTask == null)
            {
                lock (LoadMutex)
                {
                    if (_fullLoadTask == null)
                        _fullLoadTask = LoadFullAsync(cancellationToken);
                }
            }

            await _fullLoadTask.Unchain();
        }

        public async ValueTask<AnvilChunkDocument?> LoadAsync(ChunkColumnPosition columnPosition, CancellationToken cancellationToken)
        {
            await CompleteFullLoad(cancellationToken).Unchain();

            lock (_documentSet)
            {
                if (_documentSet.Remove(columnPosition, out (int Index, NbtDocument Data) tuple))
                {
                    Debug.Assert(_documentList[tuple.Index] == tuple.Data);

                    _documentList[tuple.Index] = null;
                    return new AnvilChunkDocument(columnPosition, tuple.Data);
                }
            }
            return default;
        }

        public async ValueTask<AnvilChunkDocument?> LoadAsync(int index, CancellationToken cancellationToken)
        {
            await CompleteFullLoad(cancellationToken).Unchain();

            lock (_documentSet)
            {
                NbtDocument? document = _documentList[index];
                if (document != null)
                {
                    ChunkColumnPosition columnPosition = GetColumnPosition(document.RootTag);
                    if (!_documentSet.Remove(columnPosition, out (int Index, NbtDocument Data) tuple))
                        throw new InvalidOperationException();

                    Debug.Assert(document == tuple.Data);
                    Debug.Assert(index == tuple.Index);

                    _documentList[index] = null;
                    return new AnvilChunkDocument(columnPosition, document);
                }
            }
            return default;
        }
    }

    public readonly struct AnvilChunkDocument
    {
        public ChunkColumnPosition ColumnPosition { get; }
        public NbtDocument Document { get; }

        public AnvilChunkDocument(ChunkColumnPosition columnPosition, NbtDocument document)
        {
            ColumnPosition = columnPosition;
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }
    }
}