using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MCServerSharp.AnvilStorage;
using MCServerSharp.Data.IO;
using MCServerSharp.Maths;
using MCServerSharp.NBT;

namespace MCServerSharp.World
{
    public class LocalChunkRegion : IChunkRegion
    {
        // TODO: improve

        private Stream _stream;
        private AnvilRegionReader? _regionReader;

        // change document management
        private Dictionary<ChunkColumnPosition, NbtDocument> _documents = new();

        public LocalChunkRegion()
        {
        }

        public LocalChunkRegion(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            var regionReaderStatus = AnvilRegionReader.Create(CreateReader(), out _regionReader);
            if (regionReaderStatus != OperationStatus.Done)
                throw new InvalidDataException();
        }

        private NetBinaryReader CreateReader()
        {
            return new NetBinaryReader(_stream, NetBinaryOptions.JavaDefault);
        }

        public async ValueTask<IChunkColumn?> LoadColumn(ChunkColumnManager columnManager, ChunkColumnPosition columnPosition)
        {
            if (_regionReader == null)
                return null;

            if (!_documents.TryGetValue(columnPosition, out NbtDocument? document))
            {
                AnvilChunkDocument? anvilDocument = await _regionReader.LoadAsync(columnPosition, default);
                document = anvilDocument.GetValueOrDefault().Document;
                _documents.Add(columnPosition, document);
            }

            if (document == null)
                return null;

            Debug.Assert(AnvilRegionReader.GetColumnPosition(document.RootTag) == columnPosition);

            var column = new LocalChunkColumn(columnManager, columnPosition);
            
            // TODO: move this to a Anvil parser
            {
                NbtElement level = document.RootTag["Level"];
                NbtElement sections = level["Sections"];

                column._encodedColumn = document;
                column._chunksToDecode = new(sections.GetLength());

                foreach (NbtElement section in sections.EnumerateContainer())
                {
                    int y = section["Y"].GetInt();
                    column._chunksToDecode.Add(y, section);
                }
                column._chunksToDecodeRefCount = column._chunksToDecode.Count;
            }

            return column;
        }
    }

}